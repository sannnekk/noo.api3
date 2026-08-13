using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.ThirdPartyServices.Google;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Exports;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Types;
using Noo.Api.Users.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IIntegrationRunner))]
public class IntegrationRunner : IIntegrationRunner
{
    /// <summary>
    /// How many runs may fail back-to-back before the integration is disabled. Stops a
    /// permanently broken integration from hammering Google every hour forever.
    /// </summary>
    private const int _maxConsecutiveFailures = 3;

    private readonly NooDbContext _db;

    private readonly IExportProfileRegistry _profiles;

    private readonly IGoogleTokenProvider _tokenProvider;

    private readonly IGoogleSheetsWriter _sheetsWriter;

    private readonly ILogger<IntegrationRunner> _logger;

    public IntegrationRunner(
        NooDbContext db,
        IExportProfileRegistry profiles,
        IGoogleTokenProvider tokenProvider,
        IGoogleSheetsWriter sheetsWriter,
        ILogger<IntegrationRunner> logger
    )
    {
        _db = db;
        _profiles = profiles;
        _tokenProvider = tokenProvider;
        _sheetsWriter = sheetsWriter;
        _logger = logger;
    }

    public async Task RunAsync(
        GoogleSheetsIntegrationModel integration,
        CancellationToken ct = default
    )
    {
        try
        {
            if (!await IsStillAuthorizedAsync(integration, ct))
            {
                Disable(
                    integration,
                    "У владельца интеграции больше нет прав на эту выгрузку. Интеграция отключена."
                );

                return;
            }

            var profile = _profiles.Get(integration.Type);
            var auth = await _tokenProvider.GetAuthAsync(integration.GoogleAuthData, ct);
            var data = await profile.BuildAsync(integration.Parameters, ct);

            var result = await _sheetsWriter.WriteAsync(
                auth,
                integration.SpreadsheetId,
                integration.Name,
                data,
                ct
            );

            integration.SpreadsheetId = result.SpreadsheetId;
            integration.LastRowCount = result.RowCount;
            integration.LastRunAt = Clock.Now;
            integration.LastErrorText = null;
            integration.ConsecutiveFailureCount = 0;
        }
        catch (GoogleAuthRevokedException exception)
        {
            // No amount of retrying fixes a revoked grant, so stop immediately rather than
            // burning through the failure budget first.
            _logger.LogWarning(
                exception,
                "Google access revoked for integration {IntegrationId}.",
                integration.Id
            );

            Disable(integration, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Google Sheets integration {IntegrationId} failed.",
                integration.Id
            );

            integration.LastErrorText = exception.Message;
            integration.ConsecutiveFailureCount++;

            if (integration.ConsecutiveFailureCount >= _maxConsecutiveFailures)
            {
                integration.Status = GoogleSheetsIntegrationStatus.Error;
            }
        }
        finally
        {
            integration.RunState = GoogleSheetsIntegrationRunState.Idle;
            integration.RunStartedAt = null;
            integration.NextRunAt =
                integration.Status == GoogleSheetsIntegrationStatus.Active
                    ? integration.Schedule.NextRunAt()
                    : null;
        }
    }

    /// <summary>
    /// Re-checks the owner on every run. Access granted at creation time is not evidence of
    /// access now — a mentor may have lost the student, or been blocked entirely.
    /// </summary>
    private async Task<bool> IsStillAuthorizedAsync(
        GoogleSheetsIntegrationModel integration,
        CancellationToken ct
    )
    {
        var owner = await _db.GetDbSet<UserModel>().FindAsync([integration.OwnerId], ct);

        if (owner is null || owner.IsBlocked)
        {
            return false;
        }

        var profile = _profiles.Get(integration.Type);

        return await profile.AuthorizeAsync(owner.Id, owner.Role, integration.Parameters, ct);
    }

    private static void Disable(GoogleSheetsIntegrationModel integration, string reason)
    {
        integration.Status = GoogleSheetsIntegrationStatus.Error;
        integration.LastErrorText = reason;
    }
}
