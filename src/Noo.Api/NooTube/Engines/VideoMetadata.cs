namespace Noo.Api.NooTube.Engines;

public record VideoMetadata
{
    public VideoProcessingStatus Status { get; init; }

    public string? Url { get; init; }

    public string? ThumbnailUrl { get; init; }

    public int? DurationSeconds { get; init; }
}
