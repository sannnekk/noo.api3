using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Works.Types;

namespace Noo.Api.AssignedWorks.Filters;

[PossibleSortings(
    nameof(AssignedWorkModel.Title),
    nameof(AssignedWorkModel.CreatedAt),
    nameof(AssignedWorkModel.SolvedAt),
    nameof(AssignedWorkModel.CheckedAt)
)]
public class AssignedWorkFilter : PaginationFilterBase
{
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(AssignedWorkModel.Title))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    [ArraySearchFilter]
    public IEnumerable<WorkType>? Type { get; set; }

    public AssignedWorkSolveStatus? SolveStatus { get; set; }

    public AssignedWorkCheckStatus? CheckStatus { get; set; }

    [CompareTo(nameof(AssignedWorkModel.MainMentorId))]
    [CompareTo(nameof(AssignedWorkModel.HelperMentorId))]
    public Ulid? MentorId { get; set; }

    public Ulid? StudentId { get; set; }
}
