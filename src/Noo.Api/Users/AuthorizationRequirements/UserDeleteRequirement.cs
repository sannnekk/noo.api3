using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Users.AuthorizationRequirements;

/// <summary>
/// Any user can delete their own account. Admin can delete any account.
/// </summary>
public class UserDeleteRequirement : IAuthorizationRequirement
{
    public UserRoles AdminRole => UserRoles.Admin;
}
