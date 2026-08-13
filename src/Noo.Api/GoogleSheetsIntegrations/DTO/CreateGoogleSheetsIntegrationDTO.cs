using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

public record CreateGoogleSheetsIntegrationDTO
{
    [JsonPropertyName("name")]
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [Required]
    public GoogleSheetsIntegrationType Type { get; set; }

    [JsonPropertyName("parameters")]
    public ExportParametersDTO Parameters { get; set; } = new();

    [JsonPropertyName("schedule")]
    [Required]
    public GoogleSheetsIntegrationSchedule Schedule { get; set; } =
        GoogleSheetsIntegrationSchedule.Manual;

    /// <summary>
    /// The one-time authorization code from the Google consent popup. Required on every create:
    /// each integration gets its own freshly granted refresh token rather than reusing one.
    /// </summary>
    [JsonPropertyName("googleAuthCode")]
    [Required]
    [MinLength(1)]
    public string GoogleAuthCode { get; set; } = string.Empty;

    /// <summary>
    /// The <c>state</c> handed out with the consent URL and echoed back by Google. Proves the
    /// code was obtained by this user through this platform.
    /// </summary>
    [JsonPropertyName("googleAuthState")]
    [Required]
    [MinLength(1)]
    public string GoogleAuthState { get; set; } = string.Empty;
}
