using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.ThirdPartyServices.Google;

[RegisterSingleton(typeof(IGoogleOAuthExchangeService))]
public class GoogleOAuthExchangeService : IGoogleOAuthExchangeService
{
    private const string _authEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string _tokenEndpoint = "https://oauth2.googleapis.com/token";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly GoogleConfig _config;

    public GoogleOAuthExchangeService(
        IHttpClientFactory httpClientFactory,
        IOptions<GoogleConfig> config
    )
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
    }

    public string BuildConsentUrl(string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["client_id"] = _config.ClientId;
        query["redirect_uri"] = _config.RedirectUri;
        query["response_type"] = "code";
        query["scope"] = string.Join(' ', GoogleScopes.Required);
        query["access_type"] = "offline";
        query["prompt"] = "consent";
        query["include_granted_scopes"] = "true";
        query["state"] = state;

        return $"{_authEndpoint}?{query}";
    }

    public async Task<GoogleOAuthResult> ExchangeCodeAsync(
        string code,
        CancellationToken ct = default
    )
    {
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _config.ClientId,
            ["client_secret"] = _config.ClientSecret,
            ["redirect_uri"] = _config.RedirectUri,
            ["grant_type"] = "authorization_code",
        };

        var http = _httpClientFactory.CreateClient();

        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(new Uri(_tokenEndpoint), content, ct);

        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new GoogleAuthRevokedException(
                $"Не удалось обменять код авторизации Google: {response.StatusCode} {json}"
            );
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (
            !root.TryGetProperty("refresh_token", out var refreshTokenElement)
            || refreshTokenElement.GetString() is not { Length: > 0 } refreshToken
        )
        {
            throw new GoogleAuthRevokedException(
                "Google не вернул refresh token. Убедитесь, что запрос содержит access_type=offline и prompt=consent."
            );
        }

        var accountEmail = root.TryGetProperty("id_token", out var idTokenElement)
            ? ReadEmailFromIdToken(idTokenElement.GetString())
            : null;

        return new GoogleOAuthResult(refreshToken, accountEmail);
    }

    /// <summary>
    /// Reads the <c>email</c> claim out of the id_token payload. The token comes straight from
    /// Google's token endpoint over TLS, so the signature does not need re-verifying here — and
    /// a missing or malformed email only costs us a display label.
    /// </summary>
    private static string? ReadEmailFromIdToken(string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var parts = idToken.Split('.');

        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));

            return document.RootElement.TryGetProperty("email", out var email)
                ? email.GetString()
                : null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return null;
        }
    }
}
