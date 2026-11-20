using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.WebPush;

[RegisterScoped(typeof(IPushSubscriptionRepository))]
public class PushSubscriptionRepository : Repository<PushSubscriptionModel>, IPushSubscriptionRepository
{
    public PushSubscriptionRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<List<PushSubscriptionModel>> GetForUserAsync(Ulid userId, CancellationToken ct = default)
    {
        return Context.GetDbSet<PushSubscriptionModel>()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public Task<PushSubscriptionModel?> GetByEndpointAsync(string endpoint)
    {
        return Context.GetDbSet<PushSubscriptionModel>().FirstOrDefaultAsync(x => x.Endpoint == endpoint);
    }

    public Task DeleteForUserAsync(Ulid userId, Ulid subscriptionId)
    {
        return RemoveWhereAsync(x => x.Id == subscriptionId && x.UserId == userId);
    }

    public Task DeleteByEndpointForUserAsync(Ulid userId, string endpoint)
    {
        return RemoveWhereAsync(x => x.Endpoint == endpoint && x.UserId == userId);
    }

    // Tracked removal rather than ExecuteDelete, so it joins the request's unit of work.
    private async Task RemoveWhereAsync(System.Linq.Expressions.Expression<Func<PushSubscriptionModel, bool>> predicate)
    {
        var set = Context.GetDbSet<PushSubscriptionModel>();
        var matches = await set.Where(predicate).ToListAsync();
        set.RemoveRange(matches);
    }
}
