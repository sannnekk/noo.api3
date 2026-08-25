using Ardalis.Specification;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.Access;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.QuerySpecifications;

public class CourseSpecification : Specification<CourseModel>
{
    public CourseSpecification(UserRoles? userRole, Ulid? userId, Ulid? authorId = null)
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
                // Students see exactly the courses they can open — same rule the authorization
                // handler uses, so the list and the detail page can never disagree.
                Query.Where(CourseAccessRules.AccessibleBy(userId.Value));
                break;

            default:
                // For any other roles, no courses are visible
                Query.Where(_ => false);
                break;
        }

        if (authorId.HasValue)
        {
            Query.Where(course => course.Authors.Any(author => author.Id == authorId.Value));
        }

        // Add subject to the query to include related data
        Query.Include(course => course.Subject);
        Query.Include(course => course.Thumbnail);
        // Audiences drive CourseDTO.IsPublic
        Query.Include(course => course.Audiences);
    }
}
