using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Noo.Api.Core.System.Realtime;

public class SignalRRealtimePublisher<THub, TClient> : IRealtimePublisher<TClient>
    where THub : Hub<TClient>
    where TClient : class
{
    private readonly IHubContext<THub, TClient> _hub;
    private readonly RealtimeMetrics _metrics;
    private readonly RealtimeConfig _config;

    public SignalRRealtimePublisher(
        IHubContext<THub, TClient> hub,
        RealtimeMetrics metrics,
        IOptions<RealtimeConfig> config
    )
    {
        _hub = hub;
        _metrics = metrics;
        _config = config.Value;
    }

    private static string HubName => typeof(THub).Name;

    public Task SendToUserAsync(Ulid userId, Func<TClient, Task> send, CancellationToken ct = default)
    {
        _metrics.MessageSent(HubName, "user");

        return send(_hub.Clients.User(userId.ToString()));
    }

    /// <summary>
    /// Chunked, unlike <see cref="BroadcastAsync"/>. Addressing users individually costs one
    /// backplane publish each, so a single call naming every user would put tens of thousands of
    /// messages onto Redis at once.
    /// </summary>
    public async Task SendToUsersAsync(
        IReadOnlyCollection<Ulid> userIds,
        Func<TClient, Task> send,
        CancellationToken ct = default
    )
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var ids = userIds.Select(id => id.ToString()).ToArray();

        foreach (var chunk in ids.Chunk(_config.BroadcastChunkSize))
        {
            ct.ThrowIfCancellationRequested();

            await send(_hub.Clients.Users(chunk));
            _metrics.MessageSent(HubName, "users", chunk.Length);

            if (_config.BroadcastChunkDelayMs > 0 && chunk.Length == _config.BroadcastChunkSize)
            {
                await Task.Delay(_config.BroadcastChunkDelayMs, ct);
            }
        }
    }

    public Task SendToGroupAsync(
        string group,
        Func<TClient, Task> send,
        CancellationToken ct = default
    )
    {
        _metrics.MessageSent(HubName, "group");

        return send(_hub.Clients.Group(group));
    }

    /// <summary>
    /// Not chunked, deliberately: reaching everyone is one backplane publish that each instance
    /// then writes to its own connections. Naming the recipients instead would be the expensive
    /// way to do the same thing.
    /// </summary>
    public Task BroadcastAsync(Func<TClient, Task> send, CancellationToken ct = default)
    {
        _metrics.MessageSent(HubName, "all");

        return send(_hub.Clients.All);
    }
}
