namespace Noo.Api.Core.System.Realtime.Jobs;

public enum JobProgressState
{
    Running,
    Completed,
    Failed,
}

/// <summary>
/// Progress of one long-running server operation. Generic on purpose: a Google Sheets sync and a
/// bulk notification send report the same shape, so the client needs one renderer, not one per
/// feature.
/// </summary>
/// <param name="JobId">Identifies the run, so a client that reconnects can tell whether an
/// update belongs to the operation it is still watching.</param>
/// <param name="State">Whether the run is still going, finished, or gave up.</param>
/// <param name="Processed">Items handled so far.</param>
/// <param name="Total">Items expected in total, or null when the size is not known up front.</param>
/// <param name="Message">Human-readable detail; carries the reason when <c>State</c> is Failed.</param>
public record JobProgress(
    string JobId,
    JobProgressState State,
    int Processed,
    int? Total,
    string? Message = null
)
{
    /// <summary>
    /// Completion as a fraction, or null while the total is unknown — a progress bar should show
    /// an indeterminate state rather than guess.
    /// </summary>
    public double? Fraction =>
        Total is > 0 ? Math.Clamp((double)Processed / Total.Value, 0, 1) : null;
}
