using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Users.Events;

public sealed record UserVerifiedEvent(Ulid UserId, Ulid? VerifiedById) : IDomainEvent;

public sealed class UserVerifiedUserHistoryHandler : IEventHandler<UserVerifiedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserVerifiedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserVerifiedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(@event.UserId, @event.VerifiedById, UserHistoryType.Verified);

        return Task.CompletedTask;
    }
}
