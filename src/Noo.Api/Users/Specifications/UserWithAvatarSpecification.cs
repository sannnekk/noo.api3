using Ardalis.Specification;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Specifications;

public class UserWithAvatarSpecification : Specification<UserModel>
{
    public UserWithAvatarSpecification()
    {
        Query.Include(u => u.Avatar).ThenInclude(a => a!.Media);
    }
}
