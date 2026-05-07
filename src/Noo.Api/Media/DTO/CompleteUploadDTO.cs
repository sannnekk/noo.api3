using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Media.DTO;

public record CompleteUploadDTO
{
    [Required]
    [Range(0, long.MaxValue)]
    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("etag")]
    public string? ETag { get; init; }
}
