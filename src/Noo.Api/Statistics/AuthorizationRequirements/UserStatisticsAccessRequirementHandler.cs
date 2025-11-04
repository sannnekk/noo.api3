using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Services;

namespace Noo.Api.Statistics.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class UserStatisticsAccessRequirementHandler : AuthorizationHandler<UserStatisticsAccessRequirement>
{
    private readonly IUnitOfWork _unitOfWork;

    public UserStatisticsAccessRequirementHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserStatisticsAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var currentUserRole = context.User.GetRole();
        var currentUserId = context.User.GetId();

        var targetUserIdValue = httpContext.GetRouteData().Values["userId"]?.ToString();
        if (!Ulid.TryParse(targetUserIdValue, out var targetUserId) || currentUserRole == null)
        {
            context.Fail();
            return;
        }

        if (requirement.AlwaysAllowedRoles.Contains(currentUserRole.Value))
        {
            context.Succeed(requirement);
            return;
        }

        // Student: only own statistics
        if (currentUserRole == UserRoles.Student)
        {
            if (currentUserId == targetUserId)
            {
                context.Succeed(requirement);
                return;
            }
            context.Fail();
            return;
        }

        // Mentor: self or mentored students
        if (currentUserRole == UserRoles.Mentor)
        {
            if (currentUserId == targetUserId)
            {
                context.Succeed(requirement);
                return;
            }

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
