using Microsoft.AspNetCore.Authorization;

namespace Noo.Api.Users.AuthorizationRequirements;

/// <summary>
/// User can patch only their own data
/// </summary>
public class UserPatchRequirement : IAuthorizationRequirement { }
