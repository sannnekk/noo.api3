using Ardalis.Specification;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.QuerySpecifications;

public class CourseMembershipSpecification : Specification<CourseMembershipModel>
{
    public CourseMembershipSpecification(UserRoles? userRole, Ulid? userId)
    {
        if (userRole == null || userId == null)
        {
            // If no user role or ID is provided, return no memberships
            Query.Where(_ => false);
            return;
        }

        Query.Include(m => m.Course).ThenInclude(c => c.Subject);

        switch (userRole)
        {
            case UserRoles.Admin:
            case UserRoles.Teacher:
            case UserRoles.Assistant:
                // Admins, Teachers, and Assistants can see all memberships
                Query.Include(m => m.Assigner);
                Query.Include(m => m.Student);
                Query.Where(_ => true);
                break;

            case UserRoles.Mentor:
                // Mentors can only see memberships of students they mentor
                Query
                    .Include(m => m.Student)
                    .Where(m =>
                        m.Student.MentorAssignmentsAsStudent.Any(a => a.MentorId == userId)
                    );
                break;

            case UserRoles.Student:
                // Students can see their own memberships, pinned ones first
                Query
                    .Where(membership => membership.StudentId == userId)
                    .OrderByDescending(membership => membership.IsPinnedByStudent)
                    .ThenByDescending(membership => membership.Id);
                break;

            default:
                // For any other roles, no memberships are visible
                Query.Where(_ => false);
                break;
        }
    }
}
