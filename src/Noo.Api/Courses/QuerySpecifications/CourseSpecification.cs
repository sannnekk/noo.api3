using Ardalis.Specification;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.QuerySpecifications;

public class CourseSpecification : Specification<CourseModel>
{
    public CourseSpecification(UserRoles? userRole, Ulid? userId)
    {
        if (userRole == null || userId == null)
        {
            // If no user role or ID is provided, return no courses
            Query.Where(_ => false);
            return;
        }

        switch (userRole)
        {
            case UserRoles.Admin:
            case UserRoles.Teacher:
            case UserRoles.Assistant:
            case UserRoles.Mentor:
                // Admins, Teachers, Assistants, and Mentors can see all courses
                Query.Where(_ => true);
                break;

            case UserRoles.Student:
                // Students can see courses they are enrolled in
                Query.Where(course => course.Memberships
                    .Any(membership => membership.StudentId == userId));
                break;

            default:
                // For any other roles, no courses are visible
                Query.Where(_ => false);
                break;
        }

        // Add subject to the query to include related data
        Query.Include(course => course.Subject);
    }
}
