using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.UserHistory.Services;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.Users.Events;

public sealed record UserRoleChangedEvent(
    Ulid UserId,
    Ulid? ChangedById,
    UserRoles OldRole,
    UserRoles NewRole
) : IDomainEvent;

public sealed class UserRoleChangedUserHistoryHandler : IEventHandler<UserRoleChangedEvent>
{
    private readonly IUserHistoryService _userHistoryService;

    public UserRoleChangedUserHistoryHandler(IUserHistoryService userHistoryService)
    {
        _userHistoryService = userHistoryService;
    }

    public Task HandleAsync(UserRoleChangedEvent @event, CancellationToken ct = default)
    {
        _userHistoryService.Record(
            @event.UserId,
            @event.ChangedById,
            UserHistoryType.RoleChanged,
            new Dictionary<string, string>
            {
                ["oldRole"] = @event.OldRole.ToWireName(),
                ["newRole"] = @event.NewRole.ToWireName(),
            }
        );

        return Task.CompletedTask;
    }
}
