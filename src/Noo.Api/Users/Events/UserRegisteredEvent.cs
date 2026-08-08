using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Users.Events;

public sealed record UserRegisteredEvent(Ulid UserId, string Username, UserRoles Role)
    : IDomainEvent;

public sealed class UserRegisteredUserHistoryHandler : IEventHandler<UserRegisteredEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserRegisteredUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserRegisteredEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(
            @event.UserId,
            null,
            UserHistoryType.Registered,
            new Dictionary<string, string>
            {
                ["username"] = @event.Username,
                ["role"] = @event.Role.ToWireName(),
            }
        );

        return Task.CompletedTask;
    }
}
