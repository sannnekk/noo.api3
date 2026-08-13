using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

[RegisterScoped(typeof(IGoogleSheetsIntegrationRepository))]
public class GoogleSheetsIntegrationRepository
    : Repository<GoogleSheetsIntegrationModel>,
        IGoogleSheetsIntegrationRepository
{
    public GoogleSheetsIntegrationRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public Task<List<Ulid>> GetRunnableIdsAsync(int limit, CancellationToken ct = default)
    {
        var now = Clock.Now;

        return Context
            .GetDbSet<GoogleSheetsIntegrationModel>()
            .Where(integration =>
                integration.RunState == GoogleSheetsIntegrationRunState.Queued
                || (
                    integration.RunState == GoogleSheetsIntegrationRunState.Idle
                    && integration.Status == GoogleSheetsIntegrationStatus.Active
                    && integration.NextRunAt != null
                    && integration.NextRunAt <= now
                )
            )
            .OrderBy(integration => integration.NextRunAt)
            .Take(limit)
            .Select(integration => integration.Id)
            .ToListAsync(ct);
    }

    public async Task<bool> TryClaimAsync(Ulid integrationId, CancellationToken ct = default)
    {
        // Guarding the update on the current run state makes the claim atomic: the database
        // decides the winner, so only one replica can move a given integration to Running.
        var updated = await Context
            .GetDbSet<GoogleSheetsIntegrationModel>()
            .Where(integration =>
                integration.Id == integrationId
                && integration.RunState != GoogleSheetsIntegrationRunState.Running
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(
                            integration => integration.RunState,
                            GoogleSheetsIntegrationRunState.Running
                        )
                        .SetProperty(integration => integration.RunStartedAt, Clock.Now),
                ct
            );

        return updated == 1;
    }

    public Task<int> ReclaimStaleRunsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var threshold = Clock.Now - olderThan;

        return Context
            .GetDbSet<GoogleSheetsIntegrationModel>()
            .Where(integration =>
                integration.RunState == GoogleSheetsIntegrationRunState.Running
                && integration.RunStartedAt != null
                && integration.RunStartedAt < threshold
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(
                            integration => integration.RunState,
                            GoogleSheetsIntegrationRunState.Queued
                        )
                        .SetProperty(integration => integration.RunStartedAt, (DateTime?)null),
                ct
            );
    }

}
