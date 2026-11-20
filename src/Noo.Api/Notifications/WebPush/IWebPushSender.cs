using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.WebPush;

public interface IWebPushSender
{
    public Task<WebPushSendResult> SendAsync(
        PushSubscriptionModel subscription,
        string payload,
        bool urgent,
        CancellationToken ct = default
    );
}

public enum WebPushSendResult
{
    Sent,
    Gone,
    Failed
}
