using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Filters;

[PossibleSortings(
    nameof(CourseMembershipModel.AssignerId),
    nameof(CourseMembershipModel.CreatedAt),
    nameof(CourseMembershipModel.CourseId)
)]
public class CourseMembershipFilter : PaginationFilterBase
{
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(CourseMembershipModel.Assigner.Name))]
    [CompareTo(nameof(CourseMembershipModel.Course.Name))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public Ulid? CourseId { get; set; }

    public Ulid? AssignerId { get; set; }

    public Ulid? StudentId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsArchived { get; set; }
}
