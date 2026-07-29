using Ardalis.Specification;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Specifications;

public class UserWithAvatarSpecification : Specification<UserModel>
{
    public UserWithAvatarSpecification()
    {
        Query.Include(u => u.Avatar).ThenInclude(a => a!.Media);

        // Mentors of the user, listed next to students in the user list
        Query.Include(u => u.MentorAssignmentsAsStudent).ThenInclude(a => a.Mentor);
        Query.Include(u => u.MentorAssignmentsAsStudent).ThenInclude(a => a.Subject);
    }
}
