using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Noo.Api.Core.System.Realtime.Backplane;

/// <summary>
/// Tagged <c>ready</c> and never <c>live</c>: a pod that lost the backplane should stop taking
/// new connections, but killing it would drop the ones it still serves perfectly well.
/// </summary>
public class RealtimeBackplaneHealthCheck : IHealthCheck
{
    private readonly IRealtimeBackplane _backplane;

    public RealtimeBackplaneHealthCheck(IRealtimeBackplane backplane)
    {
        _backplane = backplane;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (!_backplane.IsConfigured)
        {
            return HealthCheckResult.Healthy("No realtime backplane configured.");
        }

        try
        {
            var latency = await _backplane.Connection.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy(
                "Realtime backplane reachable.",
                new Dictionary<string, object> { { "LatencyMs", latency.TotalMilliseconds } }
            );
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Realtime backplane unreachable.", exception);
        }
    }
}
