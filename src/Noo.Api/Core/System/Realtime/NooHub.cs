using Microsoft.AspNetCore.SignalR;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Realtime;

/// <summary>
/// Base for every hub in the application. Keeps connection accounting in one place and gives
/// hubs the same view of the caller that controllers have — inject <see cref="ICurrentUser"/>
/// rather than reading <c>Context.User</c>, so hub code and HTTP code agree on who is calling.
/// </summary>
public abstract class NooHub<TClient> : Hub<TClient>
    where TClient : class
{
    private readonly RealtimeMetrics _metrics;
    private readonly RealtimeConnectionRegistry _connections;

    protected NooHub(RealtimeMetrics metrics, RealtimeConnectionRegistry connections)
    {
        _metrics = metrics;
        _connections = connections;
    }

    /// <summary>
    /// Names this hub in metrics and logs. Defaults to the concrete type name.
    /// </summary>
    protected virtual string HubName => GetType().Name;

    protected Ulid CallerId => Context.User?.GetId() ?? Ulid.Empty;

    protected UserRoles? CallerRole => Context.User?.GetRole();

    public override Task OnConnectedAsync()
    {
        _metrics.ConnectionOpened(HubName);

        // Registering here is what keeps an idle open tab counted as online once the frontend
        // stops polling — HTTP requests used to be the heartbeat, and a held socket makes none.
        if (CallerRole is { } role)
        {
            _connections.Add(CallerId, role);
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _metrics.ConnectionClosed(HubName);
        _connections.Remove(CallerId);

        return base.OnDisconnectedAsync(exception);
    }
}
