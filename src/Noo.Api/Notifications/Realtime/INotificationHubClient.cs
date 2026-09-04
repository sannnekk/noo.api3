using Noo.Api.Notifications.DTO;

namespace Noo.Api.Notifications.Realtime;

/// <summary>
/// What the server pushes down the personal channel. Method names are the wire names, so they
/// must match the TypeScript contract exactly.
/// </summary>
public interface INotificationHubClient
{
    public Task NotificationCreatedAsync(NotificationDTO notification);
}
