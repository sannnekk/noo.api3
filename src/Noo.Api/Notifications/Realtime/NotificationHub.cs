using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.System.Realtime;

namespace Noo.Api.Notifications.Realtime;

/// <summary>
/// The one always-on hub: every signed-in client holds this connection for as long as the tab is
/// open, which is what lets the frontend stop polling for unread notifications.
///
/// It exposes no invocable methods on purpose — it is a delivery channel, not an API. Anything a
/// client wants to *do* with a notification (mark read, delete) stays on the REST controller,
/// where the existing filters, policies and unit-of-work handling already apply.
/// </summary>
[Authorize]
public class NotificationHub : NooHub<INotificationHubClient>
{
    public NotificationHub(RealtimeMetrics metrics)
        : base(metrics)
    {
    }
}
