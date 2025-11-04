using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Services;

namespace Noo.Api.Users.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class UserAccessRequirementHandler : AuthorizationHandler<UserAccessRequirement>
{
    private readonly IUnitOfWork _unitOfWork;

    public UserAccessRequirementHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        Ulid? targetUserId = null;
        foreach (var key in new[] { "userId", "studentId", "mentorId" })
        {
            if (routeValues.TryGetValue(key, out var valueObj) && valueObj != null && Ulid.TryParse(valueObj.ToString(), out var parsed))
            {
                targetUserId = parsed;
                break;
            }
        }

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
        if (currentUserRole == UserRoles.Mentor)
        {
            var mentorAssignmentsRepo = _unitOfWork.MentorAssignmentRepository();
            var filter = new MentorAssignmentFilter
            {
                MentorId = currentUserId,
                StudentId = targetUserId,
                Page = 1,
                PerPage = 1
            };
            var result = await mentorAssignmentsRepo.SearchAsync(filter);
            if (result.Items.Any())
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail();
    }
}
