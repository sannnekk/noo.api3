using StackExchange.Redis;

namespace Noo.Api.Core.System.Realtime.Backplane;

public interface IRealtimeBackplane : IDisposable
{
    /// <summary>
    /// True when a backplane is configured at all. False means this instance fans out only to
    /// its own connections, which is correct for a single instance and wrong for a fleet.
    /// </summary>
    public bool IsConfigured { get; }

    /// <summary>
    /// Throws when the backplane is configured but unreachable. Unlike the cache connection,
    /// this never degrades quietly: a pod that silently loses the backplane keeps serving
    /// connections that no longer receive anything published elsewhere.
    /// </summary>
    public IConnectionMultiplexer Connection { get; }
}
