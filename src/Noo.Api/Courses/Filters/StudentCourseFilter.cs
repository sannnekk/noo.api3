using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Filters;

[PossibleSortings(nameof(CourseModel.Name), nameof(CourseModel.CreatedAt))]
public class StudentCourseFilter : PaginationFilterBase
{
    [CompareTo(nameof(CourseModel.Name))]
    [CompareTo(nameof(CourseModel.Subject.Name))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    [ArraySearchFilter]
    public IEnumerable<Ulid?>? SubjectId { get; set; }

    /// <summary>
    /// The student's own archive. It lives in course_student_state rather than on the course, so
    /// <see cref="QuerySpecifications.StudentCourseSpecification"/> applies it.
    /// </summary>
    [IgnoreFilter]
    public bool IsArchived { get; set; }
}
