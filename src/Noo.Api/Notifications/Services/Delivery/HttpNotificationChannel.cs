using AutoMapper;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Realtime;
using Noo.Api.Notifications.Types;

namespace Noo.Api.Notifications.Services.Delivery;

[RegisterScoped(typeof(INotificationChannel))]
public class HttpNotificationChannel : INotificationChannel
{
    private readonly IRealtimePublisher<INotificationHubClient> _publisher;
    private readonly IMapper _mapper;

    public HttpNotificationChannel(
        IRealtimePublisher<INotificationHubClient> publisher,
        IMapper mapper
    )
    {
        _publisher = publisher;
        _mapper = mapper;
    }

    public NotificationChannelType Channel => NotificationChannelType.Http;

    /// <summary>
    /// Reaches only the sessions that user currently has open. Delivery is best-effort by
    /// design: the notification is already persisted, so a client that was offline picks it up
    /// from the unread endpoint when it reconnects.
    /// </summary>
    public Task SendAsync(NotificationModel model, CancellationToken ct = default)
    {
        var dto = _mapper.Map<NotificationDTO>(model);

        return _publisher.SendToUserAsync(
            model.UserId,
            client => client.NotificationCreatedAsync(dto),
            ct
        );
    }
}
