using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Providers.Config;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.External.Providers;

[RegisterScoped(typeof(IExternalAuthProvider))]
public class YandexAuthProvider : IExternalAuthProvider
{
    private const string _authorizeEndpoint = "https://oauth.yandex.ru/authorize";
    private const string _tokenEndpoint = "https://oauth.yandex.ru/token";
    private const string _userInfoEndpoint = "https://login.yandex.ru/info?format=json";
    private const string _avatarTemplate = "https://avatars.yandex.net/get-yapic/{0}/islands-200";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly YandexAuthConfig _config;

    public YandexAuthProvider(IHttpClientFactory httpClientFactory, IOptions<YandexAuthConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
    }

    public ExternalAuthProviderType Type => ExternalAuthProviderType.Yandex;

    public string DisplayName => "Яндекс ID";

    public bool IsEnabled =>
        _config.Enabled
        && !string.IsNullOrWhiteSpace(_config.ClientId)
        && !string.IsNullOrWhiteSpace(_config.ClientSecret);

    public bool EmailIsTrusted => _config.TrustEmailForLinking;

    public string BuildAuthorizationUrl(ExternalAuthChallenge challenge, string codeChallenge)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["response_type"] = "code";
        query["client_id"] = _config.ClientId;
        query["redirect_uri"] = challenge.RedirectUri;
        query["state"] = challenge.State;
        // Yandex wants scopes comma-separated; VK wants them space-separated.
        query["scope"] = string.Join(',', _config.Scopes);
        query["code_challenge"] = codeChallenge;
        query["code_challenge_method"] = "S256";

        return $"{_authorizeEndpoint}?{query}";
    }

    public async Task<ExternalUserProfile> ResolveProfileAsync(
        ExternalAuthCallback callback,
        ExternalAuthChallenge challenge,
        CancellationToken ct = default
    )
    {
        var accessToken = await ExchangeCodeAsync(callback.Require("code"), challenge, ct);

        return await FetchProfileAsync(accessToken, ct);
    }

    private async Task<string> ExchangeCodeAsync(
        string code,
        ExternalAuthChallenge challenge,
        CancellationToken ct
    )
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _config.ClientId!,
            ["client_secret"] = _config.ClientSecret!,
            ["code_verifier"] = challenge.CodeVerifier,
            ["redirect_uri"] = challenge.RedirectUri,
        };

        var http = _httpClientFactory.CreateClient(ExternalAuthHttpClient.Name);

        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(new Uri(_tokenEndpoint), content, ct);

        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalAuthProviderException(
                $"Яндекс отклонил код авторизации: {response.StatusCode}."
            );
        }

        using var document = JsonDocument.Parse(json);

        return document.RootElement.TryGetProperty("access_token", out var token)
            && token.GetString() is { Length: > 0 } accessToken
            ? accessToken
            : throw new ExternalAuthProviderException("Яндекс не вернул токен доступа.");
    }

    private async Task<ExternalUserProfile> FetchProfileAsync(string accessToken, CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(ExternalAuthHttpClient.Name);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_userInfoEndpoint));
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

        using var response = await http.SendAsync(request, ct);

        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalAuthProviderException(
                $"Не удалось получить данные аккаунта Яндекс: {response.StatusCode}."
            );
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var subjectId = ReadString(root, "id");

        if (string.IsNullOrEmpty(subjectId))
        {
            throw new ExternalAuthProviderException("Яндекс не вернул идентификатор пользователя.");
        }

        return new ExternalUserProfile
        {
            SubjectId = subjectId,
            Email = ReadString(root, "default_email"),
            // Yandex only ever returns mailboxes it owns and has verified.
            EmailIsVerified = true,
            DisplayName = ReadString(root, "real_name") ?? ReadString(root, "display_name"),
            FirstName = ReadString(root, "first_name"),
            LastName = ReadString(root, "last_name"),
            AvatarUrl = ReadAvatarUrl(root),
            ProviderLogin = ReadString(root, "login"),
        };
    }

    private static string? ReadAvatarUrl(JsonElement root)
    {
        if (root.TryGetProperty("is_avatar_empty", out var isEmpty)
            && isEmpty.ValueKind == JsonValueKind.True)
        {
            return null;
        }

        var avatarId = ReadString(root, "default_avatar_id");

        return string.IsNullOrEmpty(avatarId)
            ? null
            : string.Format(_avatarTemplate, avatarId);
    }

    private static string? ReadString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            && value.GetString() is { Length: > 0 } text
            ? text
            : null;
    }
}
