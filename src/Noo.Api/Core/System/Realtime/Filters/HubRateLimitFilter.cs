using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Utils;

namespace Noo.Api.Core.System.Realtime.Filters;

/// <summary>
/// Bounds how often one connection may invoke hub methods. Hub invocations never reach the
/// HTTP rate limiter — it only sees the initial negotiate — so without this a single socket can
/// invoke without limit. A sliding window is overkill here; the point is to cap a runaway or
/// hostile client, not to meter fairly.
/// </summary>
public class HubRateLimitFilter : IHubFilter
{
    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly RealtimeConfig _config;

    public HubRateLimitFilter(IOptions<RealtimeConfig> config)
    {
        _config = config.Value;
    }

    public ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    )
    {
        if (!TryTake(invocationContext.Context.ConnectionId))
        {
            throw new HubException(
                $"Rate limit exceeded: at most {_config.InvocationsPerMinutePerConnection} calls per minute."
            );
        }

        return next(invocationContext);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next
    )
    {
        _windows.TryRemove(context.Context.ConnectionId, out _);

        return next(context, exception);
    }

    private bool TryTake(string connectionId)
    {
        var now = Clock.Now;

        var window = _windows.AddOrUpdate(
            connectionId,
            _ => new Window(now, 1),
            (_, existing) =>
                now - existing.StartedAt >= TimeSpan.FromMinutes(1)
                    ? new Window(now, 1)
                    : existing with { Count = existing.Count + 1 }
        );

        return window.Count <= _config.InvocationsPerMinutePerConnection;
    }

    private sealed record Window(DateTime StartedAt, int Count);
}
