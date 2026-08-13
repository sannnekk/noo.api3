using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Scheduling;
using Noo.Api.GoogleSheetsIntegrations.Services;

namespace Noo.Api.GoogleSheetsIntegrations.Background;

/// <summary>
/// Drives every export run — both manual ones queued from the UI and scheduled reruns. Having a
/// single path means a run behaves identically however it was triggered.
/// </summary>
[RegisterScheduledJob]
public class IntegrationDispatchJob : IScheduledJob
{
    private readonly IGoogleSheetsIntegrationRepository _integrationRepository;

    private readonly IIntegrationRunner _runner;

    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<IntegrationDispatchJob> _logger;

    public IntegrationDispatchJob(
        IGoogleSheetsIntegrationRepository integrationRepository,
        IIntegrationRunner runner,
        IUnitOfWork unitOfWork,
        ILogger<IntegrationDispatchJob> logger
    )
    {
        _integrationRepository = integrationRepository;
        _runner = runner;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public TimeSpan Interval => GoogleSheetsIntegrationConfig.DispatchInterval;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var reclaimed = await _integrationRepository.ReclaimStaleRunsAsync(
            GoogleSheetsIntegrationConfig.StaleRunThreshold,
            cancellationToken
        );

        if (reclaimed > 0)
        {
            _logger.LogWarning("Reclaimed {Count} stale Google Sheets runs.", reclaimed);
        }

        var candidates = await _integrationRepository.GetRunnableIdsAsync(
            GoogleSheetsIntegrationConfig.MaxIntegrationsPerTick,
            cancellationToken
        );

        foreach (var integrationId in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!await _integrationRepository.TryClaimAsync(integrationId, cancellationToken))
            {
                // Another replica got there first.
                continue;
            }

            var integration = await _integrationRepository.GetByIdAsync(integrationId);

            if (integration is null)
            {
                continue;
            }

            await _runner.RunAsync(integration, cancellationToken);

            // Committed per integration so a later failure cannot roll back an export that
            // already reached Google.
            await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
