using Ardalis.Specification;
using Noo.Api.Courses.Access;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.QuerySpecifications;

/// <summary>
/// One student's course list. Driven by courses rather than by membership rows, so a course reached
/// through an audience grant is indistinguishable from an assigned one.
/// </summary>
public class StudentCourseSpecification : Specification<CourseModel>
{
    public StudentCourseSpecification(Ulid studentId, bool isArchived)
    {
        Query.Where(CourseAccessRules.AccessibleBy(studentId)).Where(course => !course.IsArchived);

        // A student with no state row has archived nothing, so the two tabs have to be expressed as
        // Any/!Any rather than as a comparison against a projected flag that would be null.
        if (isArchived)
        {
            Query.Where(course =>
                course.StudentStates.Any(s => s.StudentId == studentId && s.IsArchived)
            );
        }
        else
        {
            Query.Where(course =>
                !course.StudentStates.Any(s => s.StudentId == studentId && s.IsArchived)
            );
        }

        Query
            .OrderByDescending(course =>
                course.StudentStates.Any(s => s.StudentId == studentId && s.IsPinned)
            )
            .ThenByDescending(course => course.Id);

        Query.Include(course => course.Subject);
        Query.Include(course => course.Thumbnail);
        Query.Include(course => course.Audiences);

        // Filtered includes: each collection comes back with 0 or 1 element, so the mapper can take
        // FirstOrDefault without knowing the student id.
        Query.Include(course => course.StudentStates.Where(s => s.StudentId == studentId));
        Query
            .Include(course =>
                course.Memberships.Where(m => m.StudentId == studentId && m.IsActive)
            )
            .ThenInclude(membership => membership.Assigner);
    }
}
