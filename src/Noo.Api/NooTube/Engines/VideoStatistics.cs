namespace Noo.Api.NooTube.Engines;

/// <summary>
/// Provider-agnostic playback statistics for a single video over a period.
/// </summary>
public record VideoStatistics
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public long Views { get; init; }

    public long UniqueViews { get; init; }

    public int WatchTimeSeconds { get; init; }

    public long PlayerLoads { get; init; }

    public IReadOnlyList<VideoStatisticsPoint> Timeline { get; init; } = [];
}

public record VideoStatisticsPoint
{
    public required DateTime Date { get; init; }

    public long Views { get; init; }

    public long UniqueViews { get; init; }

    public int WatchTimeSeconds { get; init; }

    public long PlayerLoads { get; init; }
}
