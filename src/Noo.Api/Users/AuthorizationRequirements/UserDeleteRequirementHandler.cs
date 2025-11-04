using Microsoft.AspNetCore.Authorization;
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

        var routeUserIdValue = httpContext.GetRouteData().Values["userId"]?.ToString();
        if (!Ulid.TryParse(routeUserIdValue, out var targetUserId))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var currentUserId = context.User.GetId();
        var currentRole = context.User.GetRole();

        if (currentRole == requirement.AdminRole || currentUserId == targetUserId)
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
