using Noo.Api.Sessions.Models;

namespace Noo.Api.Sessions.Services;

public interface ISessionService
{
    public Task<IEnumerable<SessionModel>> GetSessionsAsync(Ulid userId);
    public Task<Ulid> CreateSessionIfNotExistsAsync(HttpContext context, Ulid userId);
    public void DeleteAllSessions(Ulid userId);
    public void DeleteSession(Ulid sessionId, Ulid userId);
    public void DeleteCurrentSession(Ulid sessionId, Ulid userId);
}
