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
    public GoogleSheetsIntegrationType Type { get; set; }

    [JsonPropertyName("selectorValue")]
    public string? SelectorValue { get; set; }

    [JsonPropertyName("spreadsheetId")]
    public string? SpreadsheetId { get; set; }

    [JsonPropertyName("cronPattern")]
    [Required]
    public string CronPattern { get; set; } = string.Empty;

    // Legacy service-account based auth payload (JSON string with client_email/private_key). Optional now.
    [JsonPropertyName("googleAuthData")]
    public string? GoogleAuthData { get; set; }

    // New interactive OAuth credential details passed from frontend (auth code flow metadata).
    [JsonPropertyName("googleCredentials")]
    public GoogleOAuthCredentialsDTO? GoogleCredentials { get; set; }

    // Access token obtained on the client after OAuth consent. (Name kept as provided by the user request.)
    // NOTE: Token will be stored server-side only as part of serialized GoogleAuthData (access_token) for immediate run.
    // Subsequent runs will re-use the (possibly expired) token until refresh support is implemented.
    [JsonPropertyName("googleOAthToken")]
    public string? GoogleOAuthToken { get; set; }
}

public record GoogleOAuthCredentialsDTO
{
    [JsonPropertyName("authuser")] public string? AuthUser { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("prompt")] public string? Prompt { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
}
