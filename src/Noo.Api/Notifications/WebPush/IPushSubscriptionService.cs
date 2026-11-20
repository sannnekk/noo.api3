using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.WebPush;

public interface IPushSubscriptionService
{
    public WebPushPublicKeyDTO GetPublicKey();
    public Task<List<PushSubscriptionModel>> GetForUserAsync(Ulid userId);
    public Task<Ulid> SubscribeAsync(Ulid userId, CreatePushSubscriptionDTO dto);
    public Task UnsubscribeAsync(Ulid userId, string endpoint);
    public Task DeleteAsync(Ulid userId, Ulid subscriptionId);
    public Task SendToUserAsync(Ulid userId, WebPushPayload payload, bool urgent, CancellationToken ct = default);
}
