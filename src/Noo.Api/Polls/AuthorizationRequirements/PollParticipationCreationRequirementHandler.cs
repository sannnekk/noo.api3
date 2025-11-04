using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.Services;

namespace Noo.Api.Polls.AuthorizationRequirements;

[RegisterScoped(typeof(IAuthorizationHandler))]
public class PollParticipationCreationRequirementHandler : AuthorizationHandler<PollParticipationCreationRequirement>
{
    private readonly IUnitOfWork _unitOfWork;

    public PollParticipationCreationRequirementHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var pollRepository = _unitOfWork.PollRepository();
        var poll = await pollRepository.GetByIdAsync(pollId);
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

        var participationRepository = _unitOfWork.PollParticipationRepository();
        var userId = context.User.GetId();
        if (userId != Ulid.Empty)
        {
            var alreadyParticipated = await participationRepository.ParticipationExistsAsync(pollId, userId, null);
            if (alreadyParticipated)
            {
                context.Fail();
                return;
            }
        }

        context.Succeed(requirement);
    }
}
