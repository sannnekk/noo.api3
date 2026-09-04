using Microsoft.Extensions.Options;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Scheduling;
using Noo.Api.Sessions.Services;

namespace Noo.Api.Sessions.Background;

/// <summary>
/// Keeps everyone holding a hub connection marked online.
///
/// Presence keys expire after <c>Sessions:OnlineTtlMinutes</c> and used to be refreshed by
/// <c>SessionActivityMiddleware</c> on every request — which worked only because the frontend
/// polled. A tab that holds a socket and makes no requests would otherwise go offline while its
/// user is plainly there.
///
/// Connecting does not mark anyone online by itself: a client that opened a hub has just loaded
/// the app over HTTP, which already did. This only keeps that from lapsing.
/// </summary>
[RegisterScheduledJob]
public class RealtimePresenceHeartbeatJob : IScheduledJob
{
    private readonly RealtimeConnectionRegistry _connections;
    private readonly IOnlineService _onlineService;
    private readonly SessionConfig _options;

    public RealtimePresenceHeartbeatJob(
        RealtimeConnectionRegistry connections,
        IOnlineService onlineService,
        IOptions<SessionConfig> options
    )
    {
        _connections = connections;
        _onlineService = onlineService;
        _options = options.Value;
    }

    /// <summary>
    /// A third of the TTL, so a key survives two missed beats before anyone reads as offline.
    /// </summary>
    public TimeSpan Interval => _options.OnlineTtl / 3;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var user in _connections.Connected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _onlineService.SetUserOnlineAsync(user.UserId, user.Role);
        }
    }
}
