using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Models;
using Microsoft.EntityFrameworkCore;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(ISessionRepository))]
public class SessionRepository : Repository<SessionModel>, ISessionRepository
{
    public SessionRepository(NooDbContext context) : base(context)
    {
    }

    public void DeleteAllSessions(Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        var toRemove = set.Where(s => s.UserId == userId);
        set.RemoveRange(toRemove);
    }

    public Task<SessionModel?> GetAsync(Ulid sessionId, Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
    }

    public Task<SessionModel?> GetByDeviceIdAsync(Ulid userId, string deviceId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set
            .OrderByDescending(s => s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);
    }

    public Task<SessionModel?> GetByUserAgentAsync(Ulid userId, string userAgent)
    {
        var set = Context.GetDbSet<SessionModel>();
        return set
            .OrderByDescending(s => s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.UserAgent == userAgent);
    }

    public async Task<IEnumerable<SessionModel>> GetManyOfUserAsync(Ulid userId)
    {
        var set = Context.GetDbSet<SessionModel>();
        return await set
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync();
    }
}
