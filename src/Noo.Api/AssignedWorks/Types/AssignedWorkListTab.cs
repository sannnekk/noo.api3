namespace Noo.Api.AssignedWorks.Types;

/// <summary>
/// A slice of the assigned work list as the user sees it: one tab of the list page,
/// with its own counter next to the title. What each of them contains is defined in
/// one place, <c>AssignedWorkTabCriteria</c>.
/// </summary>
public enum AssignedWorkListTab
{
    All,
    NotSolved,
    NotChecked,
    Checked,
}
