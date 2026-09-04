using Microsoft.AspNetCore.Authorization;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils;

namespace Noo.Api.Core.System.Realtime.Ping;

/// <summary>
/// A hub that does nothing but answer, so the realtime path can be exercised end to end without
/// depending on a feature: it is what the integration tests connect to and what the k6 scenario
/// holds open to measure the cost of a connection.
/// </summary>
[Authorize]
public class RealtimePingHub : NooHub<IRealtimePingClient>
{
    private readonly ICurrentUser _currentUser;

    public RealtimePingHub(
        RealtimeMetrics metrics,
        RealtimeConnectionRegistry connections,
        ICurrentUser currentUser
    )
        : base(metrics, connections)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Answers twice on purpose: as the invocation result, and as a server push. A load test
    /// needs both to tell request latency from delivery latency.
    /// </summary>
    public async Task<RealtimePong> PingAsync()
    {
        // Deliberately ICurrentUser and not Context.User: this is the assertion that the hub
        // filter put the caller where the rest of the application looks for it.
        var pong = new RealtimePong(
            Context.ConnectionId,
            _currentUser.UserId?.ToString(),
            Clock.Now
        );

        await Clients.Caller.PongAsync(pong);

        return pong;
    }
}
