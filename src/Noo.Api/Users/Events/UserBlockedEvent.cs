using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Users.Events;

public sealed record UserBlockedEvent(Ulid UserId, Ulid? BlockedById) : IDomainEvent;

public sealed class UserBlockedUserHistoryHandler : IEventHandler<UserBlockedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserBlockedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserBlockedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(@event.UserId, @event.BlockedById, UserHistoryType.Blocked);

        return Task.CompletedTask;
    }
}
