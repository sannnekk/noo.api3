using Noo.Api.Sessions.Models;

namespace Noo.Api.Sessions.Services;

public interface ISessionService
{
    public Task<IEnumerable<SessionModel>> GetSessionsAsync(Ulid userId);
    public Task<Ulid> CreateSessionIfNotExistsAsync(HttpContext context, Ulid userId);
    public Task<bool> SessionExistsAsync(Ulid sessionId, Ulid userId);
    public Task DeleteAllSessionsAsync(Ulid userId);
    public Task DeleteSessionAsync(Ulid sessionId, Ulid userId);
    public Task DeleteCurrentSessionAsync(Ulid sessionId, Ulid userId);
    public Task DeleteSessionByIdAsync(Ulid sessionId);
}
