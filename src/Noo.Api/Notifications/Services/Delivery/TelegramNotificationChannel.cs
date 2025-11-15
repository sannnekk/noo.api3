using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Types;

namespace Noo.Api.Notifications.Services.Delivery;

[RegisterScoped(typeof(INotificationChannel))]
public class TelegramNotificationChannel : INotificationChannel
{
    public NotificationChannelType Channel => NotificationChannelType.Telegram;

    public Task SendAsync(NotificationModel model, CancellationToken ct = default)
    {
        // TODO: integrate Telegram Bot API here
        return Task.CompletedTask;
    }
}
