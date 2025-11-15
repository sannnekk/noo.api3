using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Types;

namespace Noo.Api.Notifications.Services.Delivery;

[RegisterScoped(typeof(INotificationChannel))]
public class HttpNotificationChannel : INotificationChannel
{
    public NotificationChannelType Channel => NotificationChannelType.Http;

    public Task SendAsync(NotificationModel model, CancellationToken ct = default)
    {
        // TODO: could push to SignalR/WebSocket or external webhook.
        return Task.CompletedTask;
    }
}
