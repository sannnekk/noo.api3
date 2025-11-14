using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Request;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Users.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class UserDeleteRequirementHandler : AuthorizationHandler<UserDeleteRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserDeleteRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var targetUserId = httpContext.GetRouteData().Values.GetUlidValue("userId");

        var currentUserId = context.User.GetId();
        var currentRole = context.User.GetRole();

        if (currentRole == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (requirement.AlwaysAllowedRoles.Contains(currentRole.Value) || currentUserId == targetUserId)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
