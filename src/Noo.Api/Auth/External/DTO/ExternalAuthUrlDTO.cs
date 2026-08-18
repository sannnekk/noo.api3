using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Auth.External.DTO;

public record ExternalAuthUrlDTO
{
    /// <summary>The provider's authorization page. The browser is sent here as a full-page redirect.</summary>
    [Required]
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
}
