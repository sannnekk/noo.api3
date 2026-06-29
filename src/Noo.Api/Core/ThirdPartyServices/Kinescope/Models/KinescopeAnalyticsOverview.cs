using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record KinescopeAnalyticsOverview
{
    [JsonPropertyName("total")]
    public KinescopeAnalyticsPoint? Total { get; init; }

    [JsonPropertyName("timeline")]
    public IReadOnlyList<KinescopeAnalyticsPoint> Timeline { get; init; } = [];
}

public record KinescopeAnalyticsPoint
{
    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; init; }

    [JsonPropertyName("views")]
    public long Views { get; init; }

    [JsonPropertyName("unique_views")]
    public long UniqueViews { get; init; }

    [JsonPropertyName("watch_time")]
    public double WatchTime { get; init; }

    [JsonPropertyName("duration")]
    public double Duration { get; init; }

    [JsonPropertyName("player_loads")]
    public long PlayerLoads { get; init; }

    [JsonPropertyName("online")]
    public long Online { get; init; }
}
