using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// Google credentials persisted with an integration.
/// Only a refresh token is kept, encrypted at rest via <see cref="Noo.Api.Core.Security.ISecretProtector"/>.
/// Access tokens are short-lived and are minted on demand by <see cref="IGoogleTokenProvider"/>.
/// </summary>
public struct GoogleAuthData
{
    [JsonPropertyName("refresh_token_encrypted")]
    public string RefreshTokenEncrypted { get; set; }

    /// <summary>
    /// The Google account that granted consent, shown in the UI so users can tell integrations apart.
    /// </summary>
    [JsonPropertyName("account_email")]
    public string? AccountEmail { get; set; }

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; }

    public static GoogleAuthData Deserialize(string v)
    {
        return JsonSerializer.Deserialize<GoogleAuthData>(v);
    }

    public readonly string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
