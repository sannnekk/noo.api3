using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Noo.Api.Core.System.HealthChecks;

// TODO: rethink the memory check
public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var availableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var allocatedMemory = GC.GetTotalMemory(false);

        var availableMemoryInMb = availableMemory / 1024 / 1024;
        var allocatedMemoryInMb = allocatedMemory / 1024 / 1024;

        var ratio = (double)allocatedMemory / availableMemory;

        var status =
            ratio > 0.9 ? HealthStatus.Unhealthy
            : ratio > 0.75 ? HealthStatus.Degraded
            : HealthStatus.Healthy;

        var data = new MemoryHealthCheckResult(allocatedMemoryInMb, availableMemoryInMb, ratio);

        return Task.FromResult(
            new HealthCheckResult(status, "Memory check", null, data.ToDictionary())
        );
    }
}
