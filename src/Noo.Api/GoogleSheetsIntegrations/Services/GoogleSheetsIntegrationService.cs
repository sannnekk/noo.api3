using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Exports;
using Noo.Api.GoogleSheetsIntegrations.Filters;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Specifications;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IGoogleSheetsIntegrationService))]
public class GoogleSheetsIntegrationService : IGoogleSheetsIntegrationService
{
    private static readonly TimeSpan _stateLifetime = TimeSpan.FromMinutes(30);

    private readonly IGoogleSheetsIntegrationRepository _integrationRepository;

    private readonly IExportProfileRegistry _profiles;

    private readonly IGoogleOAuthExchangeService _oauthExchange;

    private readonly ISecretProtector _secretProtector;

    private readonly IHashService _hashService;

    private readonly ICurrentUser _currentUser;

    private readonly IMapper _mapper;

    public GoogleSheetsIntegrationService(
        IGoogleSheetsIntegrationRepository integrationRepository,
        IExportProfileRegistry profiles,
        IGoogleOAuthExchangeService oauthExchange,
        ISecretProtector secretProtector,
        IHashService hashService,
        ICurrentUser currentUser,
        IMapper mapper
    )
    {
        _integrationRepository = integrationRepository;
        _profiles = profiles;
        _oauthExchange = oauthExchange;
        _secretProtector = secretProtector;
        _hashService = hashService;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public GoogleOAuthUrlDTO CreateOAuthUrl()
    {
        var state = CreateState(_currentUser.RequireUserId());

        return new GoogleOAuthUrlDTO
        {
            Url = _oauthExchange.BuildConsentUrl(state),
            State = state,
        };
    }

    public Task<SearchResult<GoogleSheetsIntegrationModel>> GetIntegrationsAsync(
        GoogleSheetsIntegrationFilter filter
    )
    {
        // An integration is its owner's: it holds their Google grant and writes to their
        // spreadsheet, so nobody else has business listing it, whatever their role.
        var specification = new IntegrationsByOwnerSpecification(_currentUser.RequireUserId());

        return _integrationRepository.SearchAsync(filter, [specification]);
    }

    public async Task<Ulid> CreateIntegrationAsync(
        CreateGoogleSheetsIntegrationDTO request,
        CancellationToken ct = default
    )
    {
        var userId = _currentUser.RequireUserId();
        var role = _currentUser.RequireUserRole();

        if (!IsStateValid(request.GoogleAuthState, userId))
        {
            throw new BadRequestException(
                "Сессия авторизации Google недействительна или истекла. Подключите аккаунт заново."
            );
        }

        var profile = _profiles.Get(request.Type);
        var parameters = _mapper.Map<ExportParameters>(request.Parameters);

        profile.Validate(parameters);

        if (!await profile.AuthorizeAsync(userId, role, parameters, ct))
        {
            throw new ForbiddenException();
        }

        var oauth = await _oauthExchange.ExchangeCodeAsync(request.GoogleAuthCode, ct);

        var model = new GoogleSheetsIntegrationModel
        {
            Name = request.Name,
            Type = request.Type,
            Parameters = parameters,
            Schedule = request.Schedule,
            NextRunAt = request.Schedule.NextRunAt(),
            OwnerId = userId,
            GoogleAuthData = new GoogleAuthData
            {
                RefreshTokenEncrypted = _secretProtector.Protect(oauth.RefreshToken),
                AccountEmail = oauth.AccountEmail,
                Scopes = GoogleScopes.Required,
            },
        };

        _integrationRepository.Add(model);

        return model.Id;
    }

    public async Task UpdateIntegrationAsync(
        Ulid integrationId,
        UpdateGoogleSheetsIntegrationDTO request
    )
    {
        // Error is a state the dispatcher assigns after repeated failures, not something a
        // client may claim for itself.
        if (request.Status == GoogleSheetsIntegrationStatus.Error)
        {
            throw new BadRequestException("Статус «Ошибка» нельзя установить вручную.");
        }

        var integration = await RequireAccessAsync(integrationId);

        integration.Name = request.Name ?? integration.Name;
        integration.Schedule = request.Schedule ?? integration.Schedule;

        if (request.Status is { } status)
        {
            // Re-enabling clears the failure streak, so a previously broken integration gets
            // judged on its next run rather than being disabled again immediately.
            if (
                status == GoogleSheetsIntegrationStatus.Active
                && integration.Status != GoogleSheetsIntegrationStatus.Active
            )
            {
                integration.ConsecutiveFailureCount = 0;
                integration.LastErrorText = null;
            }

            integration.Status = status;
        }

        integration.NextRunAt =
            integration.Status == GoogleSheetsIntegrationStatus.Active
                ? integration.Schedule.NextRunAt()
                : null;
    }

    public async Task QueueIntegrationAsync(Ulid integrationId, CancellationToken ct = default)
    {
        var integration = await RequireAccessAsync(integrationId);

        // Only queue an idle integration: asking again while a run is already in flight should
        // not stack up a second one. No atomic guard is needed here — the dispatcher's claim is
        // what actually decides who runs it.
        if (integration.RunState == GoogleSheetsIntegrationRunState.Idle)
        {
            integration.RunState = GoogleSheetsIntegrationRunState.Queued;
        }
    }

    public async Task DeleteIntegrationAsync(Ulid integrationId)
    {
        await RequireAccessAsync(integrationId);

        _integrationRepository.DeleteById(integrationId);
    }

    /// <summary>
    /// The integration, if it is the caller's own. Running one spends its owner's Google
    /// grant and rewrites their spreadsheet, and editing or deleting one takes it away from
    /// them — none of which is anyone else's to do, however senior.
    /// </summary>
    private async Task<GoogleSheetsIntegrationModel> RequireAccessAsync(Ulid integrationId)
    {
        var integration =
            await _integrationRepository.GetByIdAsync(integrationId)
            ?? throw new NotFoundException();

        if (integration.OwnerId != _currentUser.RequireUserId())
        {
            throw new ForbiddenException();
        }

        return integration;
    }

    /// <summary>
    /// Binds the OAuth state to the requesting user and to a moment in time, so an authorization
    /// code obtained through someone else's consent screen cannot be attached to this account.
    /// </summary>
    private string CreateState(Ulid userId)
    {
        var payload = $"{userId}:{Clock.Now.Ticks}";
        var signature = _hashService.Hash(payload);

        return $"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload))}.{signature}";
    }

    internal bool IsStateValid(string state, Ulid userId)
    {
        var parts = state.Split('.');

        if (parts.Length != 2)
        {
            return false;
        }

        string payload;

        try
        {
            payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
        }
        catch (FormatException)
        {
            return false;
        }

        if (!_hashService.Verify(payload, parts[1]))
        {
            return false;
        }

        var fields = payload.Split(':');

        return fields.Length == 2
            && fields[0] == userId.ToString()
            && long.TryParse(fields[1], out var ticks)
            && Clock.Now - new DateTime(ticks) < _stateLifetime;
    }
}
