using Ardalis.Specification;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.QuerySpecifications;

public class MentorAssignmentSpecification : Specification<MentorAssignmentModel>
{
    public MentorAssignmentSpecification(Ulid mentorId)
    {
        Query
            .Include(assignment => assignment.Mentor)
            .Include(assignment => assignment.Subject)
            .Include(assignment => assignment.Student)
            .Where(assignment => assignment.MentorId == mentorId);
    }
}
