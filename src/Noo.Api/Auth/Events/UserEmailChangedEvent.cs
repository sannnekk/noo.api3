using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Auth.Events;

/// <summary>
/// Raised once the change is confirmed, not when it is requested — an unconfirmed request
/// changes nothing about the account.
/// </summary>
public sealed record UserEmailChangedEvent(Ulid UserId, string OldEmail, string NewEmail)
    : IDomainEvent;

public sealed class UserEmailChangedUserHistoryHandler : IEventHandler<UserEmailChangedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserEmailChangedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserEmailChangedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(
            @event.UserId,
            null,
            UserHistoryType.EmailChanged,
            new Dictionary<string, string>
            {
                ["oldEmail"] = @event.OldEmail,
                ["newEmail"] = @event.NewEmail,
            }
        );

        return Task.CompletedTask;
    }
}
