using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Utils;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(ISessionService))]
public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISessionRepository _sessionRepository;
    private readonly IMapper _mapper;

    public SessionService(IUnitOfWork unitOfWork, ISessionRepository sessionRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
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
            await _unitOfWork.CommitAsync();
            return incoming.Id;
        }

        // Update metadata on existing session
        existing.LastRequestAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UserAgent = incoming.UserAgent;
        existing.Browser = incoming.Browser;
        existing.Os = incoming.Os;
        existing.Device = incoming.Device;
        existing.DeviceType = incoming.DeviceType;
        existing.IpAddress = incoming.IpAddress;
        existing.DeviceId = incoming.DeviceId ?? existing.DeviceId;

        _sessionRepository.Update(existing);
        await _unitOfWork.CommitAsync();
        return existing.Id;
    }

    public Task DeleteAllSessionsAsync(Ulid userId)
    {
        _sessionRepository.DeleteAllSessions(userId);
        return _unitOfWork.CommitAsync();
    }

    public async Task DeleteSessionAsync(Ulid sessionId, Ulid userId)
    {
        var model = await _sessionRepository.GetAsync(sessionId, userId);

        model.ThrowNotFoundIfNull();

        _sessionRepository.Delete(model);
        await _unitOfWork.CommitAsync();
    }

    public Task<IEnumerable<SessionModel>> GetSessionsAsync(Ulid userId)
    {
        return _sessionRepository.GetManyOfUserAsync(userId);
    }
}
