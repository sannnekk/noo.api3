namespace Noo.Api.GoogleSheetsIntegrations;

public static class GoogleSheetsIntegrationConfig
{
    /// <summary>
    /// How often the dispatcher looks for due or queued integrations. Also the worst-case delay
    /// between pressing "run" and the export starting.
    /// </summary>
    public static readonly TimeSpan DispatchInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// A run held for longer than this is presumed abandoned — the replica that claimed it
    /// died — and is returned to the queue.
    /// </summary>
    public static readonly TimeSpan StaleRunThreshold = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Integrations processed per tick. Kept low deliberately: exports are long and Google
    /// enforces per-user write quotas.
    /// </summary>
    public const int MaxIntegrationsPerTick = 2;
}
