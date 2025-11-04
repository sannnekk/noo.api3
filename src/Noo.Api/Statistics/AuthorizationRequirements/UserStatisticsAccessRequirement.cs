using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Statistics.AuthorizationRequirements;

public class UserStatisticsAccessRequirement : IAuthorizationRequirement
{
    public IEnumerable<UserRoles> AlwaysAllowedRoles { get; } = [
        UserRoles.Admin,
        UserRoles.Teacher,
        UserRoles.Assistant
    ];
}
