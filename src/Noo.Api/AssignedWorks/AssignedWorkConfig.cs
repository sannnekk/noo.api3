namespace Noo.Api.AssignedWorks;

public static class AssignedWorkConfig
{
    /// <summary>
    /// Maximum time allowed for shifting the solve deadline.
    /// </summary>
    public static readonly TimeSpan MaxSolveDeadlineShift = TimeSpan.FromDays(3);

    /// <summary>
    /// Maximum time allowed for shifting the check deadline.
    /// </summary>
    public static readonly TimeSpan MaxCheckDeadlineShift = TimeSpan.FromDays(3);
}
