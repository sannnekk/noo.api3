using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Types;

namespace Noo.Api.Sessions.Services;

public interface ISessionRepository : IRepository<SessionModel>
{
    public void DeleteAllSessions(Ulid userId);
    public bool DeleteSession(Ulid sessionId, Ulid userId);
    public Task<SessionModel?> GetAsync(Ulid sessionId, Ulid userId);
    public Task<bool> ExistsAsync(Ulid sessionId, Ulid userId);
    public Task<SessionModel?> GetByDeviceIdAsync(Ulid userId, string deviceId);
    public Task<SessionModel?> GetByUserAgentAsync(Ulid userId, string userAgent);
    public Task<IEnumerable<SessionModel>> GetManyOfUserAsync(Ulid userId);

    /// <summary>
    /// Counts the distinct users active in the given period per browser. A user active on several
    /// browsers is counted once in each of them.
    /// </summary>
    public Task<IReadOnlyList<BrowserUserCount>> GetUserCountByBrowserAsync(
        DateTime from,
        DateTime to
    );

    /// <summary>
    /// Counts the distinct users active in the given period per kind of device. A user active on
    /// several kinds of device is counted once in each of them.
    /// </summary>
    public Task<IReadOnlyList<DeviceTypeUserCount>> GetUserCountByDeviceTypeAsync(
        DateTime from,
        DateTime to
    );
}
