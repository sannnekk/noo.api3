using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Media.DTO;

public record DownloadUrlDTO
{
    [Required]
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
