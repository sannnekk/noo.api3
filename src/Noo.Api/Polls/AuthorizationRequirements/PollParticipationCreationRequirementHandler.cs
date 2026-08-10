using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.Services;

namespace Noo.Api.Polls.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class PollParticipationCreationRequirementHandler : AuthorizationHandler<PollParticipationCreationRequirement>
{
    private readonly IPollRepository _pollRepository;

    public PollParticipationCreationRequirementHandler(IPollRepository pollRepository)
    {
        _pollRepository = pollRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PollParticipationCreationRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            context.Fail();
            return;
        }

        var pollIdValue = httpContext.GetRouteData().Values["pollId"]?.ToString();
        if (!Ulid.TryParse(pollIdValue, out var pollId))
        {
            context.Fail();
            return;
        }

        var poll = await _pollRepository.GetByIdAsync(pollId);
        if (poll == null)
        {
            context.Fail();
            return;
        }

        if (!poll.IsActive)
        {
            context.Fail();
            return;
        }

        if (poll.ExpiresAt.HasValue && poll.ExpiresAt.Value <= Clock.Now)
        {
            context.Fail();
            return;
        }

        if (poll.IsAuthRequired && !(httpContext.User.Identity?.IsAuthenticated ?? false))
        {
            context.Fail();
            return;
        }

        // Whether the caller already voted is deliberately not checked here:
        // PollService answers that with UserAlreadyVotedException, which tells
        // the client what happened instead of a blanket 403.
        context.Succeed(requirement);
    }
}
