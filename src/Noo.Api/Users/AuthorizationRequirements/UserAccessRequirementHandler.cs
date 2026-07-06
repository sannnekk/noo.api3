using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Request;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Services;

namespace Noo.Api.Users.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class UserAccessRequirementHandler : AuthorizationHandler<UserAccessRequirement>
{
    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;

    public UserAccessRequirementHandler(IMentorAssignmentRepository mentorAssignmentRepository)
    {
        _mentorAssignmentRepository = mentorAssignmentRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserAccessRequirement requirement
    )
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var currentUserRole = context.User.GetRole();
        var currentUserId = context.User.GetId();

        if (currentUserRole == null)
        {
            context.Fail();
            return;
        }

        // Fast path for privileged roles
        if (requirement.AlwaysAllowedRoles.Contains(currentUserRole.Value))
        {
            context.Succeed(requirement);
            return;
        }

        // Every endpoint guarded by this requirement must expose the target user id
        // under the route key "userId" — any other name makes self access fail.
        // This contract is enforced by UserAccessRouteContractTests.
        var routeValues = httpContext.GetRouteData().Values;
        var targetUserId = routeValues.GetUlidValue("userId");

        if (targetUserId is null)
        {
            context.Fail();
            return;
        }

        // Self access allowed for any authenticated (non-blocked enforced elsewhere)
        if (targetUserId == currentUserId)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
