using System.Text.Json;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.ThirdPartyServices.Google;

public interface IGoogleOAuthExchangeService
{
    public Task<(string accessToken, string refreshToken)> ExchangeCodeAsync(string code, CancellationToken ct = default);
}

[RegisterSingleton(typeof(IGoogleOAuthExchangeService))]
public class GoogleOAuthExchangeService : IGoogleOAuthExchangeService
{
    private readonly HttpClient _http; // transient via factory
    private readonly GoogleOAuthConfig _config;

    public GoogleOAuthExchangeService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _http = httpClientFactory.CreateClient();
        _config = configuration.GetSection("GoogleOAuth").Get<GoogleOAuthConfig>() ?? new GoogleOAuthConfig();
    }

    public async Task<(string accessToken, string refreshToken)> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_config.ClientId) || string.IsNullOrWhiteSpace(_config.ClientSecret))
        {
            throw new InvalidOperationException("Google OAuth configuration missing ClientId/ClientSecret");
        }

        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _config.ClientId,
            ["client_secret"] = _config.ClientSecret,
            ["redirect_uri"] = _config.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Google OAuth token exchange failed: {response.StatusCode} {json}");
        }
        var doc = JsonDocument.Parse(json).RootElement;
        var accessToken = doc.GetProperty("access_token").GetString()!;
        var refreshToken = doc.TryGetProperty("refresh_token", out var rf) ? rf.GetString() : null;
        if (refreshToken is null)
        {
            throw new Exception("No refresh_token returned by Google (ensure access_type=offline & prompt=consent in auth request)");
        }
        return (accessToken, refreshToken);
    }
}
