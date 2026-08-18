using System.Linq.Expressions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;

namespace Noo.Api.AssignedWorks.Specifications;

public static class AssignedWorkTabCriteria
{
    /// <summary>
    /// The one definition of what a tab of the assigned work list contains. Both the list
    /// query (through <see cref="AssignedWorkSearchSpecification"/>) and the tab counters
    /// (<see cref="Services.IAssignedWorkRepository.GetCountsForUserAsync"/>) are built from
    /// it, so a counter cannot disagree with the rows its tab shows.
    /// </summary>
    /// <remarks>
    /// A work being solved or being checked right now still belongs to the unsolved
    /// respectively unchecked tab — it is not done yet, and that is where its owner
    /// goes looking for it.
    /// </remarks>
    public static Expression<Func<AssignedWorkModel, bool>> For(AssignedWorkListTab tab) =>
        tab switch
        {
            AssignedWorkListTab.NotSolved => aw =>
                !AssignedWorkStatuses.Solved.Contains(aw.SolveStatus),
            AssignedWorkListTab.NotChecked => aw =>
                AssignedWorkStatuses.Solved.Contains(aw.SolveStatus)
                && !AssignedWorkStatuses.Checked.Contains(aw.CheckStatus),
            AssignedWorkListTab.Checked => aw =>
                AssignedWorkStatuses.Checked.Contains(aw.CheckStatus),
            // All, and anything a client sends that is none of the above.
            _ => _ => true,
        };
}
