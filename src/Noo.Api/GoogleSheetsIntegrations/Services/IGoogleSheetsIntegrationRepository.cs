using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

public interface IGoogleSheetsIntegrationRepository : IRepository<GoogleSheetsIntegrationModel>
{
    /// <summary>
    /// Ids of integrations that should run now: manually queued ones, and active ones whose
    /// schedule has come due.
    /// </summary>
    public Task<List<Ulid>> GetRunnableIdsAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Atomically takes ownership of an integration for running. Returns false when another
    /// replica claimed it first, which is what keeps a multi-replica deployment from
    /// double-exporting.
    /// </summary>
    public Task<bool> TryClaimAsync(Ulid integrationId, CancellationToken ct = default);

    /// <summary>
    /// Returns runs abandoned mid-flight (their replica died) back to the queue.
    /// </summary>
    public Task<int> ReclaimStaleRunsAsync(TimeSpan olderThan, CancellationToken ct = default);
}
