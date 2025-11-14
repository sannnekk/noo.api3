using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Sessions.Models;

namespace Noo.Api.Sessions.Services;

public interface ISessionRepository : IRepository<SessionModel>
{
    public void DeleteAllSessions(Ulid userId);
    public Task<SessionModel?> GetAsync(Ulid sessionId, Ulid userId);
    public Task<SessionModel?> GetByDeviceIdAsync(Ulid userId, string deviceId);
    public Task<SessionModel?> GetByUserAgentAsync(Ulid userId, string userAgent);
    public Task<IEnumerable<SessionModel>> GetManyOfUserAsync(Ulid userId);
}
