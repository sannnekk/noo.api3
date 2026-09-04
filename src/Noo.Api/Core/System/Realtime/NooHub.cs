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

    protected NooHub(RealtimeMetrics metrics)
    {
        _metrics = metrics;
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

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _metrics.ConnectionClosed(HubName);

        return base.OnDisconnectedAsync(exception);
    }
}
