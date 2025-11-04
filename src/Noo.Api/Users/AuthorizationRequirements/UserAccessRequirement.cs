using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Users.AuthorizationRequirements;

/// <summary>
/// Access to a user resource (reading user data or mentor/student assignments).
/// Rules:
/// - Admin/Teacher/Assistant: can access anybody.
/// - Student: only self.
/// - Mentor: self or any student they are assigned to (mentor assignments table).
/// </summary>
public class UserAccessRequirement : IAuthorizationRequirement
{
    public IEnumerable<UserRoles> AlwaysAllowedRoles { get; } = [
        UserRoles.Admin,
        UserRoles.Teacher,
        UserRoles.Assistant
    ];
}
