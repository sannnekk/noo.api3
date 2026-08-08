using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Auth.Events;

public sealed record UserEmailConfirmedEvent(Ulid UserId) : IDomainEvent;

public sealed class UserEmailConfirmedUserHistoryHandler : IEventHandler<UserEmailConfirmedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserEmailConfirmedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserEmailConfirmedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(@event.UserId, null, UserHistoryType.EmailConfirmed);

        return Task.CompletedTask;
    }
}
