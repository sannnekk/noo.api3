using Ardalis.Specification;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.QuerySpecifications;

public class StudentMentorAssignmentSpecification : Specification<MentorAssignmentModel>
{
    public StudentMentorAssignmentSpecification(Ulid studentId)
    {
        Query
            .Include(assignment => assignment.Mentor)
            .Include(assignment => assignment.Subject)
            .Include(assignment => assignment.Student)
            .Where(assignment => assignment.StudentId == studentId);
    }
}
