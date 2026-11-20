using System.Text.Json.Serialization;

namespace Noo.Api.Core.System.HealthChecks;

public record MemoryHealthCheckResult
{
    [JsonPropertyName("allocatedMemoryInMb")]
    public double AllocatedMemoryInMb { get; init; }

    [JsonPropertyName("availableMemoryInMb")]
    public double AvailableMemoryInMb { get; init; }

    [JsonPropertyName("ratio")]
    public double Ratio { get; init; }

    public MemoryHealthCheckResult(
        double allocatedMemoryInMb,
        double availableMemoryInMb,
        double ratio
    )
    {
        AllocatedMemoryInMb = allocatedMemoryInMb;
        AvailableMemoryInMb = availableMemoryInMb;
        Ratio = ratio;
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "allocatedMemoryInMb", AllocatedMemoryInMb },
            { "availableMemoryInMb", AvailableMemoryInMb },
            { "ratio", Ratio },
        };
    }
}
