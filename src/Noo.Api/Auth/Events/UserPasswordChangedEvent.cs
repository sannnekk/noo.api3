using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Auth.Events;

/// <summary>
/// <c>ViaReset</c> is true when the password was set through the forgotten-password flow rather
/// than changed by a signed-in user. That distinction is what makes the entry worth auditing.
/// </summary>
public sealed record UserPasswordChangedEvent(Ulid UserId, bool ViaReset) : IDomainEvent;

public sealed class UserPasswordChangedUserHistoryHandler
    : IEventHandler<UserPasswordChangedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserPasswordChangedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserPasswordChangedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(
            @event.UserId,
            null,
            @event.ViaReset ? UserHistoryType.PasswordReset : UserHistoryType.PasswordChanged
        );

        return Task.CompletedTask;
    }
}
