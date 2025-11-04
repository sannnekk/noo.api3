using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Polls.AuthorizationRequirements;

public class PollParticipationAccessRequirement : IAuthorizationRequirement
{
    public IEnumerable<UserRoles> AlwaysAllowedRoles { get; } = [
        UserRoles.Admin,
        UserRoles.Teacher
    ];
}
