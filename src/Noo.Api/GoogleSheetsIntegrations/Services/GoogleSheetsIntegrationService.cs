using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Exceptions;
using Noo.Api.GoogleSheetsIntegrations.Filters;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IGoogleSheetsIntegrationService))]
public class GoogleSheetsIntegrationService : IGoogleSheetsIntegrationService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IGoogleSheetsIntegrationRepository _integrationRepository;

    private readonly IMapper _mapper;

    private readonly IGoogleAuthService _googleAuth;

    private readonly IGoogleSheetsService _googleSheets;

    private readonly IPollDataCollector _pollDataCollector;

    private readonly IUserDataCollector _userDataCollector;
    private readonly IGoogleOAuthExchangeService _oauthExchange;

    public GoogleSheetsIntegrationService(
        IUnitOfWork unitOfWork,
        IGoogleSheetsIntegrationRepository integrationRepository,
        IMapper mapper,
        IGoogleAuthService googleAuth,
        IGoogleSheetsService googleSheets,
        IPollDataCollector pollDataCollector,
        IUserDataCollector userDataCollector,
        IGoogleOAuthExchangeService oauthExchange
    )
    {
        _unitOfWork = unitOfWork;
        _integrationRepository = integrationRepository;
        _mapper = mapper;
        _googleAuth = googleAuth;
        _googleSheets = googleSheets;
        _pollDataCollector = pollDataCollector;
        _userDataCollector = userDataCollector;
        _oauthExchange = oauthExchange;
    }

    public async Task<Ulid> CreateIntegrationAsync(CreateGoogleSheetsIntegrationDTO request)
    {
        var model = _mapper.Map<GoogleSheetsIntegrationModel>(request);

        // Build GoogleAuthData. Preference: explicit service-account json string if provided.
        if (!string.IsNullOrWhiteSpace(request.GoogleAuthData))
        {
            try
            {
                model.GoogleAuthData = GoogleAuthData.Deserialize(request.GoogleAuthData!);
            }
            catch
            {
                throw new BadRequestException("Invalid googleAuthData JSON");
            }
        }
        else if (request.GoogleCredentials is not null && !string.IsNullOrWhiteSpace(request.GoogleCredentials.Code))
        {
            // Exchange auth code for tokens
            var (accessToken, refreshToken) = await _oauthExchange.ExchangeCodeAsync(request.GoogleCredentials.Code);
            model.GoogleAuthData = new GoogleAuthData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        else
        {
            throw new BadRequestException("Provide either service account googleAuthData or googleCredentials.code");
        }

        _integrationRepository.Add(model);
        await _unitOfWork.CommitAsync();

        return model.Id;
    }

    public Task DeleteIntegrationAsync(Ulid integrationId)
    {
        _integrationRepository.DeleteById(integrationId);
        return _unitOfWork.CommitAsync();
    }

    public Task<SearchResult<GoogleSheetsIntegrationModel>> GetIntegrationsAsync(GoogleSheetsIntegrationFilter filter)
    {
        return _integrationRepository.SearchAsync(filter);
    }

    public async Task RunIntegrationAsync(Ulid integrationId)
    {
        var integration = await _integrationRepository.GetByIdAsync(integrationId);

        if (integration is null)
        {
            throw new NotFoundException();
        }

        try
        {
            // TODO: Implement refresh token exchange for new access token when expired.
            // Currently using stored access token (or service account data) directly.
            var auth = await _googleAuth.AuthenticateAsync(integration.GoogleAuthData);
            var data = await PrepareDataAsync(integration);

            if (integration.SpreadsheetId is null)
            {
                var sheet = _googleSheets.CreateSheet(auth, integration.Name);

                sheet.AddTags(GoogleSheetsIntegrationConfig.SheetTags);
                sheet.AddTable(data);

                integration.SpreadsheetId = await _googleSheets.SaveAsync(auth, sheet);
            }
            else
            {
                var sheet = await _googleSheets.GetSheetAsync(auth, integration.SpreadsheetId);
                sheet.UpdateTable(data);
                await _googleSheets.SaveAsync(auth, sheet);
            }

            integration.LastRunAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();
        }
        catch (Exception exception)
        {
            integration.LastErrorText = exception.Message ?? "Unknown error";
            await _unitOfWork.CommitAsync();

            throw new GoogleServiceException();
        }
    }

    private Task<DataTable> PrepareDataAsync(GoogleSheetsIntegrationModel integration)
    {
        switch (integration.Type)
        {
            case GoogleSheetsIntegrationType.UserRole:
                var role = Enum.Parse<UserRoles>(integration.SelectorValue!);
                return _userDataCollector.GetUsersFromRoleAsync(role);
            case GoogleSheetsIntegrationType.UserCourse:
                var courseId = Ulid.Parse(integration.SelectorValue);
                return _userDataCollector.GetUsersFromCourseAsync(courseId);
            case GoogleSheetsIntegrationType.UserWork:
                var workId = Ulid.Parse(integration.SelectorValue);
                return _userDataCollector.GetUsersFromWorkAsync(workId);
            case GoogleSheetsIntegrationType.PollResults:
                var pollId = Ulid.Parse(integration.SelectorValue);
                return _pollDataCollector.GetPollResultsAsync(pollId);
            default:
                throw new UnknownDataSelectorException();
        }
    }
}
