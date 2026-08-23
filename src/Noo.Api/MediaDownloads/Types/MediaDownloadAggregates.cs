namespace Noo.Api.MediaDownloads.Types;

// Both types use init-only properties rather than positional parameters on purpose: EF projects
// a grouping into a member initializer happily, but cannot translate one projected into a
// constructor call — something the InMemory provider used in tests accepts and MySQL rejects.

/// <summary>
/// Download totals for one file, as aggregated by the database.
/// </summary>
public record MediaDownloadCounts
{
    public Ulid MediaId { get; init; }
    public int TotalDownloads { get; init; }
    public int UniqueUsers { get; init; }
    public DateTime LastDownloadAt { get; init; }
}

/// <summary>
/// One user's download activity on a material, as aggregated by the database.
/// </summary>
public record MediaDownloaderCounts
{
    public Ulid UserId { get; init; }
    public int DownloadCount { get; init; }
    public DateTime FirstDownloadAt { get; init; }
    public DateTime LastDownloadAt { get; init; }
}
