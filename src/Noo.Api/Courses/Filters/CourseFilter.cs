using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Courses.Models;
using Noo.Api.Users.Filters;

namespace Noo.Api.Courses.Filters;

[PossibleSortings(
    nameof(CourseModel.Name),
    nameof(CourseModel.CreatedAt),
    nameof(CourseModel.SubjectId)
)]
public class CourseFilter : PaginationFilterBase
{
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(CourseModel.Name))]
    [CompareTo(nameof(CourseModel.Subject.Name))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }

    public Ulid? SubjectId { get; set; }

    public UserFilter? Authors { get; set; }
}
