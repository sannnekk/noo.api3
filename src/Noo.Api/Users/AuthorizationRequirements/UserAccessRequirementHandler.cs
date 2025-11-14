using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Services;
using Noo.Api.Core.Request;

namespace Noo.Api.Users.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class UserAccessRequirementHandler : AuthorizationHandler<UserAccessRequirement>
{
    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;

    public UserAccessRequirementHandler(IMentorAssignmentRepository mentorAssignmentRepository)
    {
        _mentorAssignmentRepository = mentorAssignmentRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserAccessRequirement requirement)
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

        // Potential route keys that map to a target user id
        // userId -> used in /user/{userId}
        // studentId -> mentor assignments (student perspective)
        // mentorId -> mentor assignments (mentor perspective)
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

        // Mentor: can access data of assigned students
        if (currentUserRole == UserRoles.Mentor && await _mentorAssignmentRepository.GetAsync(currentUserId, targetUserId.Value) != null)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
