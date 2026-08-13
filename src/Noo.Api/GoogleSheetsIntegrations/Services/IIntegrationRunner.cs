using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Services;

public interface IIntegrationRunner
{
    /// <summary>
    /// Runs one already-claimed integration and records the outcome on it. Does not throw for
    /// export failures — a failed run is recorded on the integration, not propagated, so one
    /// bad integration cannot stop the dispatcher from servicing the rest.
    /// </summary>
    public Task RunAsync(GoogleSheetsIntegrationModel integration, CancellationToken ct = default);
}
