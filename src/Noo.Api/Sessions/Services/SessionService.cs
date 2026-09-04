using AutoMapper;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Utils;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(ISessionService))]
public class SessionService : ISessionService
{
    /// <summary>
    /// How long a known-good session is trusted without asking the database. Every deletion
    /// path drops the key, so this is only the window in which a session that vanished by some
    /// other means still authenticates.
    /// </summary>
    private static readonly TimeSpan _existsCacheTtl = TimeSpan.FromSeconds(60);

    private readonly ISessionRepository _sessionRepository;
    private readonly ICacheRepository _cache;
    private readonly IMapper _mapper;

    public SessionService(
        ISessionRepository sessionRepository,
        ICacheRepository cache,
        IMapper mapper
    )
    {
        _mapper = mapper;
        _cache = cache;
        _sessionRepository = sessionRepository;
    }

    private static string ExistsKey(Ulid sessionId) => $"session:exists:{sessionId}";

    public async Task<Ulid> CreateSessionIfNotExistsAsync(HttpContext context, Ulid userId)
    {
        if (context is null || context.User is null)
        {
            throw new ArgumentNullException(nameof(context), "HttpContext or User cannot be null.");
        }

        var incoming = context.AsSessionModel(userId);

        // Deduplicate: prefer deviceId when present; else fallback to user agent for browsers
        SessionModel? existing = null;

        if (!string.IsNullOrWhiteSpace(incoming.DeviceId))
        {
            existing = await _sessionRepository.GetByDeviceIdAsync(userId, incoming.DeviceId);
        }
        else if (!string.IsNullOrWhiteSpace(incoming.UserAgent))
        {
            existing = await _sessionRepository.GetByUserAgentAsync(userId, incoming.UserAgent);
        }

        if (existing is null)
        {
            _sessionRepository.Add(incoming);
            return incoming.Id;
        }

        // Update metadata on existing session
        existing.LastRequestAt = Clock.Now;
        existing.UpdatedAt = Clock.Now;
        existing.UserAgent = incoming.UserAgent;
        existing.Browser = incoming.Browser;
        existing.Os = incoming.Os;
        existing.Device = incoming.Device;
        existing.DeviceType = incoming.DeviceType;
        existing.IpAddress = incoming.IpAddress;
        existing.DeviceId = incoming.DeviceId ?? existing.DeviceId;

        return existing.Id;
    }

    public async Task<bool> SessionExistsAsync(Ulid sessionId, Ulid userId)
    {
        // Only hits are cached, and keyed by session alone so that a deletion knowing just the
        // session id can still drop them. A miss costs exactly what the uncached call did.
        var cachedOwner = await _cache.GetAsync<string>(ExistsKey(sessionId));

        if (cachedOwner is not null)
        {
            return cachedOwner == userId.ToString();
        }

        var exists = await _sessionRepository.ExistsAsync(sessionId, userId);

        if (exists)
        {
            await _cache.SetAsync(ExistsKey(sessionId), userId.ToString(), _existsCacheTtl);
        }

        return exists;
    }

    public async Task DeleteAllSessionsAsync(Ulid userId)
    {
        // Read the ids before removing them: this is the "sign out everywhere" path behind a
        // password reset, so leaving stale keys behind would keep other devices authenticated.
        var sessions = await _sessionRepository.GetManyOfUserAsync(userId);

        _sessionRepository.DeleteAllSessions(userId);

        await Task.WhenAll(sessions.Select(session => _cache.RemoveAsync(ExistsKey(session.Id))));
    }

    public async Task DeleteSessionAsync(Ulid sessionId, Ulid userId)
    {
        if (!_sessionRepository.DeleteSession(sessionId, userId))
        {
            throw new NotFoundException();
        }

        await _cache.RemoveAsync(ExistsKey(sessionId));
    }

    public async Task DeleteCurrentSessionAsync(Ulid sessionId, Ulid userId)
    {
        _sessionRepository.DeleteSession(sessionId, userId);

        await _cache.RemoveAsync(ExistsKey(sessionId));
    }

    public async Task DeleteSessionByIdAsync(Ulid sessionId)
    {
        _sessionRepository.DeleteById(sessionId);

        await _cache.RemoveAsync(ExistsKey(sessionId));
    }

    public Task<IEnumerable<SessionModel>> GetSessionsAsync(Ulid userId)
    {
        return _sessionRepository.GetManyOfUserAsync(userId);
    }
}
