using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;

namespace Noo.Api.Courses.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class CourseAccessRequirementHandler : AuthorizationHandler<CourseAccessRequirement>
{
    private readonly ICourseMembershipService _membershipService;

    public CourseAccessRequirementHandler(ICourseMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var courseIdValue = httpContext.GetRouteData().Values["courseId"]?.ToString();
        var userRole = context.User.GetRole();
        var userId = context.User.GetId();

        if (!Ulid.TryParse(courseIdValue, out var courseId) || userRole == null)
        {
            context.Fail();
            return;
        }

        if (requirement.AlwaysAllowedRoles.ToList().Contains(userRole.Value))
        {
            context.Succeed(requirement);
            return;
        }

        if (userRole == UserRoles.Student)
        {
            bool hasAccess = await _membershipService.HasAccessAsync(courseId, userId);

            if (hasAccess)
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail();
    }
}
