namespace Noo.Api.AssignedWorks.Types;

/// <summary>
/// The outcome of scoring the automatically checkable tasks of a work:
/// <paramref name="Score"/> is their total, and <paramref name="IsComplete"/> tells whether
/// they were all of the work's tasks, so no mentor has to look at it.
/// </summary>
public readonly record struct TaskCheckResult(int Score, bool IsComplete);
