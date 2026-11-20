using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.WebPush;

public interface IPushSubscriptionRepository : IRepository<PushSubscriptionModel>
{
    public Task<List<PushSubscriptionModel>> GetForUserAsync(Ulid userId, CancellationToken ct = default);
    public Task<PushSubscriptionModel?> GetByEndpointAsync(string endpoint);
    public Task DeleteForUserAsync(Ulid userId, Ulid subscriptionId);
    public Task DeleteByEndpointForUserAsync(Ulid userId, string endpoint);
}
