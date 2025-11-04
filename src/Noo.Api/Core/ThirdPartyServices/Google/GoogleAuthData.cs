
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// Authentication data for Google Sheets service account access.
/// Only the required subset of service account JSON is stored to reduce persisted secret surface.
/// PrivateKey should be the full PEM key text (including BEGIN/END markers) with newlines preserved ("\n" may be used and will be normalized).
/// </summary>
public struct GoogleAuthData
{
    [JsonPropertyName("client_email")] public string? ClientEmail { get; set; }

    [JsonPropertyName("private_key")] public string? PrivateKey { get; set; }

    // Optional direct OAuth access token (interactive user auth). If present and no service-account fields,
    // we build a credential for user-based access.
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }

    // Persisted refresh token for long-lived access; used to mint new access tokens when executing integrations.
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

    public static GoogleAuthData Deserialize(string v)
    {
        return JsonSerializer.Deserialize<GoogleAuthData>(v);
    }

    public readonly string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
