using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Types;
using Noo.Api.Notifications.WebPush;

namespace Noo.Api.Notifications.Services.Delivery;

[RegisterScoped(typeof(INotificationChannel))]
public class WebPushNotificationChannel : INotificationChannel
{
    private readonly IPushSubscriptionService _pushSubscriptions;

    public WebPushNotificationChannel(IPushSubscriptionService pushSubscriptions)
    {
        _pushSubscriptions = pushSubscriptions;
    }

    public NotificationChannelType Channel => NotificationChannelType.WebPush;

    public Task SendAsync(NotificationModel model, CancellationToken ct = default)
    {
        var payload = new WebPushPayload
        {
            Id = model.Id.ToString(),
            Type = model.Type,
            Title = model.Title,
            Body = model.Message,
            Link = model.Link,
        };

        return _pushSubscriptions.SendToUserAsync(model.UserId, payload, model.IsBanner, ct);
    }
}
