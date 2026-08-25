using System.Linq.Expressions;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Types;

namespace Noo.Api.Courses.Access;

/// <summary>
/// The one definition of "which courses may this student reach". Both the authorization handler and
/// every student-facing list query compile this expression, so a course can never be listed but not
/// openable, or the reverse.
/// </summary>
public static class CourseAccessRules
{
    /// <summary>
    /// Access comes either from an explicit membership or from an audience grant. Only
    /// <see cref="CourseAudienceKind.Everyone"/> is evaluated today, so a row of any other kind
    /// grants nothing.
    /// </summary>
    public static Expression<Func<CourseModel, bool>> AccessibleBy(Ulid studentId)
    {
        return course =>
            !course.IsDeleted
            && (
                course.Memberships.Any(m => m.StudentId == studentId && m.IsActive)
                || course.Audiences.Any(a => a.Kind == CourseAudienceKind.Everyone)
            );
    }
}
