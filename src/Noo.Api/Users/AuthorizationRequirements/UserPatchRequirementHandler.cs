using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Users.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class UserPatchRequirementHandler : AuthorizationHandler<UserPatchRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserPatchRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var userIdValue = httpContext.GetRouteData().Values["userId"]?.ToString();
        if (!Ulid.TryParse(userIdValue, out var targetUserId))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var currentUserId = context.User.GetId();

        if (currentUserId == targetUserId)
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
