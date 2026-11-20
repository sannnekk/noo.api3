namespace Noo.Api.MediaDownloads.Types;

/// <summary>
/// Download totals for one file, as aggregated by the database
/// </summary>
public record MediaDownloadCounts
{
    public Ulid MediaId { get; init; }
    public int TotalDownloads { get; init; }
    public int UniqueUsers { get; init; }
    public DateTime LastDownloadAt { get; init; }
}

/// <summary>
/// One user's download activity on a material, as aggregated by the database
/// </summary>
public record MediaDownloaderCounts
{
    public Ulid UserId { get; init; }
    public int DownloadCount { get; init; }
    public DateTime FirstDownloadAt { get; init; }
    public DateTime LastDownloadAt { get; init; }
}
