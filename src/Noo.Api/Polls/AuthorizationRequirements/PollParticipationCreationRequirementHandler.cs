using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.Services;

namespace Noo.Api.Polls.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class PollParticipationCreationRequirementHandler : AuthorizationHandler<PollParticipationCreationRequirement>
{
    private readonly IPollRepository _pollRepository;
    private readonly IPollParticipationRepository _participationRepository;

    public PollParticipationCreationRequirementHandler(IPollRepository pollRepository, IPollParticipationRepository participationRepository)
    {
        _pollRepository = pollRepository;
        _participationRepository = participationRepository;
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

        if (poll.ExpiresAt != default && poll.ExpiresAt <= DateTime.UtcNow)
        {
            context.Fail();
            return;
        }

        if (poll.IsAuthRequired && !(httpContext.User.Identity?.IsAuthenticated ?? false))
        {
            context.Fail();
            return;
        }

        var userId = context.User.GetId();
        if (userId != Ulid.Empty)
        {
            var alreadyParticipated = await _participationRepository.ParticipationExistsAsync(pollId, userId, null);
            if (alreadyParticipated)
            {
                context.Fail();
                return;
            }
        }

        context.Succeed(requirement);
    }
}
