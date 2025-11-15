using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.Services;

namespace Noo.Api.Polls.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class PollParticipationAccessRequirementHandler : AuthorizationHandler<PollParticipationAccessRequirement>
{
    private readonly IPollParticipationRepository _pollParticipationRepository;

    public PollParticipationAccessRequirementHandler(IPollParticipationRepository pollParticipationRepository)
    {
        _pollParticipationRepository = pollParticipationRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PollParticipationAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var userRole = context.User.GetRole();

        if (userRole == null)
        {
            context.Fail();
            return;
        }

        // Teachers and admins can access any participation
        if (requirement.AlwaysAllowedRoles.Contains(userRole.Value))
        {
            context.Succeed(requirement);
            return;
        }

        // Other authenticated users can access only their own participation
        var participationIdValue = httpContext.GetRouteData().Values["participationId"]?.ToString();
        if (!Ulid.TryParse(participationIdValue, out var participationId))
        {
            context.Fail();
            return;
        }

        var participation = await _pollParticipationRepository.GetByIdAsync(participationId);
        if (participation == null)
        {
            context.Fail();
            return;
        }

        var userId = context.User.GetId();
        if (participation.UserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
