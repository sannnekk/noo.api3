using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.DTO;

public record CreateNooTubeVideoDTO
{
    [JsonPropertyName("title")]
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("serviceType")]
    public NooTubeServiceType ServiceType { get; set; } = NooTubeServiceType.Kinescope;

    [JsonPropertyName("description")]
    [MaxLength(512)]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnailId")]
    public Ulid? ThumbnailId { get; set; }

    [JsonPropertyName("fileSize")]
    [Required]
    [Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    [JsonPropertyName("fileName")]
    [MaxLength(255)]
    public string? FileName { get; set; }

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = false;

    [Required]
    [JsonPropertyName("isListed")]
    public bool IsListed { get; set; } = false;

    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}
