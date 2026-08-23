using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;

namespace Noo.Api.MediaDownloads.DTO;

/// <summary>
/// Download totals for one file attached to a course material.
/// </summary>
public record MaterialFileDownloadSummaryDTO : IHasPresignedMedia
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "MaterialFileDownloadSummary";

    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(Media);
    }

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id => Media.Id;

    [Required]
    [JsonPropertyName("media")]
    public MediaDTO Media { get; init; } = default!;

    [Required]
    [JsonPropertyName("totalDownloads")]
    public int TotalDownloads { get; init; }

    [Required]
    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; init; }

    [JsonPropertyName("lastDownloadAt")]
    public DateTime? LastDownloadAt { get; init; }

    // The row stands for the file, so it carries the file's own timestamps.
    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt => Media.CreatedAt;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt => Media.UpdatedAt;
}
