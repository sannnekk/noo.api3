using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Types;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(ISessionRepository))]
public class SessionRepository : Repository<SessionModel>, ISessionRepository
{
    public SessionRepository(NooDbContext context)
        : base(context) { }

    public void DeleteAllSessions(Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        var toRemove = set.Where(s => s.UserId == userId);
        set.RemoveRange(toRemove);
    }

    public bool DeleteSession(Ulid sessionId, Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        var entity = set.FirstOrDefault(s => s.Id == sessionId && s.UserId == userId);
        if (entity == null)
        {
            return false;
        }

        set.Remove(entity);
        return true;
    }

    public Task<SessionModel?> GetAsync(Ulid sessionId, Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
    }

    public Task<bool> ExistsAsync(Ulid sessionId, Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set.AnyAsync(s => s.Id == sessionId && s.UserId == userId);
    }

    public Task<SessionModel?> GetByDeviceIdAsync(Ulid userId, string deviceId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set.OrderByDescending(s => s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);
    }

    public Task<SessionModel?> GetByUserAgentAsync(Ulid userId, string userAgent)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set.OrderByDescending(s => s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.UserAgent == userAgent);
    }

    public async Task<IEnumerable<SessionModel>> GetManyOfUserAsync(Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return await set.Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<BrowserUserCount>> GetUserCountByBrowserAsync(
        DateTime from,
        DateTime to
    )
    {
        return await UserCountByBrowserQuery(from, to).ToListAsync();
    }

    public async Task<IReadOnlyList<DeviceTypeUserCount>> GetUserCountByDeviceTypeAsync(
        DateTime from,
        DateTime to
    )
    {
        return await UserCountByDeviceTypeQuery(from, to).ToListAsync();
    }

    // The aggregate queries are shaped separately from their materialization so a test can compile
    // them to SQL without a database — the InMemory provider the other tests run on evaluates
    // anything on the client and so cannot tell a translatable grouping from an untranslatable one.

    internal IQueryable<BrowserUserCount> UserCountByBrowserQuery(DateTime from, DateTime to)
    {
        return ActiveInPeriod(from, to)
            .GroupBy(s => s.Browser)
            .Select(g => new BrowserUserCount
            {
                Browser = g.Key,
                UserCount = g.Select(s => s.UserId).Distinct().Count(),
            });
    }

    internal IQueryable<DeviceTypeUserCount> UserCountByDeviceTypeQuery(DateTime from, DateTime to)
    {
        return ActiveInPeriod(from, to)
            .GroupBy(s => s.DeviceType)
            .Select(g => new DeviceTypeUserCount
            {
                DeviceType = g.Key,
                UserCount = g.Select(s => s.UserId).Distinct().Count(),
            });
    }

    private IQueryable<SessionModel> ActiveInPeriod(DateTime from, DateTime to)
    {
        return Context.GetDbSet<SessionModel>()
            .Where(s => s.LastRequestAt != null && s.LastRequestAt >= from && s.LastRequestAt <= to);
    }
}
