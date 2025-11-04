using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Users.AuthorizationRequirements;

/// <summary>
/// Teachers/Admin can assign any mentor. Mentors can assign only themselves as mentor to a student.
/// </summary>
public class MentorAssignRequirement : IAuthorizationRequirement
{
    public IEnumerable<UserRoles> ElevatedRoles { get; } = [
        UserRoles.Admin,
        UserRoles.Teacher
    ];
}
