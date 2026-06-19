using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.NooTube.DTO;

public record NooTubeVideoUploadDTO
{
    [Required]
    [JsonPropertyName("videoId")]
    public Ulid VideoId { get; init; }

    [Required]
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("externalId")]
    public string ExternalId { get; init; } = string.Empty;
}
