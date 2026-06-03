using AutoMapper;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Utils;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(ISessionService))]
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMapper _mapper;

    public SessionService(ISessionRepository sessionRepository, IMapper mapper)
    {
        _mapper = mapper;
        _sessionRepository = sessionRepository;
    }

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

    public Task<bool> SessionExistsAsync(Ulid sessionId, Ulid userId)
    {
        return _sessionRepository.ExistsAsync(sessionId, userId);
    }

    public void DeleteAllSessions(Ulid userId)
    {
        _sessionRepository.DeleteAllSessions(userId);
    }

    public void DeleteSession(Ulid sessionId, Ulid userId)
    {
        if (!_sessionRepository.DeleteSession(sessionId, userId))
        {
            throw new NotFoundException();
        }
    }

    public void DeleteCurrentSession(Ulid sessionId, Ulid userId)
    {
        _sessionRepository.DeleteSession(sessionId, userId);
    }

    public void DeleteSessionById(Ulid sessionId)
    {
        _sessionRepository.DeleteById(sessionId);
    }

    public Task<IEnumerable<SessionModel>> GetSessionsAsync(Ulid userId)
    {
        return _sessionRepository.GetManyOfUserAsync(userId);
    }
}
