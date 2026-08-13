using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

public record GoogleOAuthUrlDTO
{
    [Required]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Opaque value the frontend must hand back with the authorization code, so a code obtained
    /// in someone else's browser cannot be replayed into this user's account.
    /// </summary>
    [Required]
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}
