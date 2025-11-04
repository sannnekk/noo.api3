using Microsoft.AspNetCore.Authorization;

namespace Noo.Api.Users.AuthorizationRequirements;

/// <summary>
/// User can patch only their own data (no role-based escalation per TODO).
/// </summary>
public class UserPatchRequirement : IAuthorizationRequirement { }
