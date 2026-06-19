using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.DTO;

public record UpdateNooTubeVideoDTO
{
    [JsonPropertyName("title")]
    [MinLength(1)]
    [MaxLength(255)]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    [MaxLength(512)]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnailId")]
    public Ulid? ThumbnailId { get; set; }

    [JsonPropertyName("externalIdentifier")]
    [MaxLength(255)]
    public string? ExternalIdentifier { get; set; }

    [JsonPropertyName("externalUrl")]
    [MaxLength(512)]
    public string? ExternalUrl { get; set; }

    [JsonPropertyName("externalThumbnailUrl")]
    [MaxLength(512)]
    public string? ExternalThumbnailUrl { get; set; }

    [JsonPropertyName("serviceType")]
    public NooTubeServiceType? ServiceType { get; set; }

    [JsonPropertyName("state")]
    public VideoState? State { get; set; }

    [JsonPropertyName("duration")]
    [Range(0, 16_777_215)]
    public int? Duration { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("isListed")]
    public bool? IsListed { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}
