namespace Noo.Api.GoogleSheetsIntegrations.Types;

/// <summary>
/// Where an integration is in its run cycle. Deliberately separate from
/// <see cref="GoogleSheetsIntegrationStatus"/>: a run must never overwrite whether the user
/// has the integration enabled.
/// </summary>
public enum GoogleSheetsIntegrationRunState
{
    Idle,

    /// <summary>
    /// Waiting to be picked up by the dispatcher, either from a manual run or a due schedule.
    /// </summary>
    Queued,

    /// <summary>
    /// Claimed by a dispatcher. Claiming is a conditional update, so only one replica can hold it.
    /// </summary>
    Running,
}
