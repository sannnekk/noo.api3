using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Users.Events;

/// <summary>
/// Raised when a user's profile fields are patched. <paramref name="ChangedFields"/> holds the
/// patched JSON paths — the values themselves are deliberately not recorded, since a profile can
/// carry personal data that has no business being duplicated into an audit log.
/// </summary>
public sealed record UserProfileUpdatedEvent(
    Ulid UserId,
    Ulid? ChangedById,
    IReadOnlyCollection<string> ChangedFields
) : IDomainEvent;

public sealed class UserProfileUpdatedUserHistoryHandler : IEventHandler<UserProfileUpdatedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserProfileUpdatedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserProfileUpdatedEvent @event, CancellationToken ct = default)
    {
        if (@event.ChangedFields.Count == 0)
        {
            return Task.CompletedTask;
        }

        _userHistoryService.Record(
            @event.UserId,
            @event.ChangedById,
            UserHistoryType.ProfileUpdated,
            new Dictionary<string, string> { ["fields"] = string.Join(", ", @event.ChangedFields) }
        );

        return Task.CompletedTask;
    }
}
