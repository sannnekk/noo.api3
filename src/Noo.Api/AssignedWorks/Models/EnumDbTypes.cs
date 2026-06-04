namespace Noo.Api.AssignedWorks.Models;

public static class AssignedWorkEnumDbDataTypes
{
    public const string AssignedWorkHistoryType =
        "ENUM('Created', 'StartedSolving', 'SolveDeadlineShifted', 'Solved', 'StartedChecking', 'CheckDeadlineShifted', 'Checked', 'SentOnRecheck', 'SentOnResolve', 'HelperMentorAdded', 'MainMentorChanged')";

    public const string AssignedWorkSolveStatus = "ENUM('NotSolved', 'InProgress', 'Solved')";

    public const string AssignedWorkCheckStatus = "ENUM('NotChecked', 'InProgress', 'Checked')";

    public const string AssignedWorkAnswerStatus = "ENUM('NotSubmitted', 'Submitted')";
}
