namespace Noo.Api.AssignedWorks.Types;

/// <summary>
/// The one place that knows which solve/check statuses mean "done" and how a work
/// reaches one of them. Both terminal solve statuses mean the work was handed in and
/// all three terminal check statuses mean it was checked, so code asking that question
/// goes through here instead of listing the members again and drifting apart.
/// </summary>
public static class AssignedWorkStatuses
{
    public static readonly IReadOnlyList<AssignedWorkSolveStatus> Solved =
    [
        AssignedWorkSolveStatus.SolvedInDeadline,
        AssignedWorkSolveStatus.SolvedAfterDeadline,
    ];

    public static readonly IReadOnlyList<AssignedWorkSolveStatus> Unsolved =
    [
        AssignedWorkSolveStatus.NotSolved,
        AssignedWorkSolveStatus.InProgress,
    ];

    public static readonly IReadOnlyList<AssignedWorkCheckStatus> Checked =
    [
        AssignedWorkCheckStatus.CheckedInDeadline,
        AssignedWorkCheckStatus.CheckedAfterDeadline,
        AssignedWorkCheckStatus.CheckedAutomatically,
    ];

    public static readonly IReadOnlyList<AssignedWorkCheckStatus> Unchecked =
    [
        AssignedWorkCheckStatus.NotChecked,
        AssignedWorkCheckStatus.InProgress,
    ];

    /// <summary>
    /// The status of a work handed in at <paramref name="solvedAt"/>, a Moscow wall-clock
    /// moment as produced by <see cref="Core.Utils.Clock"/>.
    /// </summary>
    public static AssignedWorkSolveStatus SolvedAt(DateTime solvedAt, DateTime? deadlineAt) =>
        IsLate(solvedAt, deadlineAt)
            ? AssignedWorkSolveStatus.SolvedAfterDeadline
            : AssignedWorkSolveStatus.SolvedInDeadline;

    /// <summary>
    /// The status of a work checked at <paramref name="checkedAt"/>, a Moscow wall-clock
    /// moment as produced by <see cref="Core.Utils.Clock"/>.
    /// </summary>
    public static AssignedWorkCheckStatus CheckedAt(DateTime checkedAt, DateTime? deadlineAt) =>
        IsLate(checkedAt, deadlineAt)
            ? AssignedWorkCheckStatus.CheckedAfterDeadline
            : AssignedWorkCheckStatus.CheckedInDeadline;

    /// <summary>
    /// A work without a deadline can never be late.
    /// </summary>
    private static bool IsLate(DateTime at, DateTime? deadlineAt) =>
        deadlineAt.HasValue && at > deadlineAt.Value;
}
