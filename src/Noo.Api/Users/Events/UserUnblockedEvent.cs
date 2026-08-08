using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Users.Events;

public sealed record UserUnblockedEvent(Ulid UserId, Ulid? UnblockedById) : IDomainEvent;

public sealed class UserUnblockedUserHistoryHandler : IEventHandler<UserUnblockedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserUnblockedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserUnblockedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(@event.UserId, @event.UnblockedById, UserHistoryType.Unblocked);

        return Task.CompletedTask;
    }
}
