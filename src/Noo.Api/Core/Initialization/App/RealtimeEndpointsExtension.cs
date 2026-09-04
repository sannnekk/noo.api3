using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Noo.Api.Core.System.Realtime;

namespace Noo.Api.Core.Initialization.App;

public static class RealtimeEndpointsExtension
{
    /// <summary>
    /// Every hub lives under this prefix so the JWT handler, the tracing filter and the ingress
    /// can all recognise realtime traffic by path alone.
    /// </summary>
    public const string HubPathPrefix = "/hubs";

    /// <summary>
    /// The one place hubs are mapped. Hubs themselves live in the module that owns them; they
    /// are listed here so the set of realtime entry points can be read off a single file.
    /// </summary>
    public static WebApplication MapNooHubs(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<RealtimeConfig>>().Value.Enabled)
        {
            return app;
        }

        // No hubs yet — the transport lands before the first feature that uses it.

        return app;
    }

    /// <summary>
    /// Hub endpoints opt out of the global limiter: it counts every request, which would throttle
    /// the negotiate handshake and shred the long-polling transport. Invocations are bounded by
    /// their own per-connection limiter instead.
    /// </summary>
    public static HubEndpointConventionBuilder MapNooHub<THub>(
        this WebApplication app,
        string pattern
    )
        where THub : Hub
    {
        var builder = app.MapHub<THub>($"{HubPathPrefix}{pattern}");

        builder.DisableRateLimiting();

        return builder;
    }
}
