using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;
using Noo.Api.Users.DTO;

namespace Noo.Api.MediaDownloads.DTO;

/// <summary>
/// How often one user downloaded a material's files, and when they last did.
/// </summary>
public record MaterialFileDownloaderDTO : IHasPresignedMedia
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "MaterialFileDownloader";

    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(User);
    }

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id => UserId;

    [Required]
    [JsonPropertyName("userId")]
    public Ulid UserId { get; init; }

    [JsonPropertyName("user")]
    public UserDTO? User { get; init; }

    [Required]
    [JsonPropertyName("downloadCount")]
    public int DownloadCount { get; init; }

    [Required]
    [JsonPropertyName("firstDownloadAt")]
    public DateTime FirstDownloadAt { get; init; }

    [Required]
    [JsonPropertyName("lastDownloadAt")]
    public DateTime LastDownloadAt { get; init; }

    // The row aggregates a user's downloads, so its span is when they first and last downloaded.
    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt => FirstDownloadAt;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt => LastDownloadAt;
}
