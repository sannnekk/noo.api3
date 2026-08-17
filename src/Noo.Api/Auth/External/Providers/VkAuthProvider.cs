using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Providers.Config;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.External.Providers;

[RegisterScoped(typeof(IExternalAuthProvider))]
public class VkAuthProvider : IExternalAuthProvider
{
    private const string _authorizeEndpoint = "https://id.vk.ru/authorize";
    private const string _tokenEndpoint = "https://id.vk.ru/oauth2/auth";
    private const string _userInfoEndpoint = "https://id.vk.ru/oauth2/user_info";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly VkAuthConfig _config;

    public VkAuthProvider(IHttpClientFactory httpClientFactory, IOptions<VkAuthConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
    }

    public ExternalAuthProviderType Type => ExternalAuthProviderType.Vk;

    public string DisplayName => "VK ID";

    public bool IsEnabled => _config.Enabled && !string.IsNullOrWhiteSpace(_config.ClientId);

    public bool EmailIsTrusted => _config.TrustEmailForLinking;

    public string BuildAuthorizationUrl(ExternalAuthChallenge challenge, string codeChallenge)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["response_type"] = "code";
        query["client_id"] = _config.ClientId;
        query["redirect_uri"] = challenge.RedirectUri;
        query["state"] = challenge.State;
        query["scope"] = string.Join(' ', _config.Scopes);
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
        // VK returns device_id alongside code and requires it back at the token endpoint.
        var accessToken = await ExchangeCodeAsync(
            callback.Require("code"),
            callback.Require("device_id"),
            challenge,
            ct
        );

        return await FetchProfileAsync(accessToken, ct);
    }

    private async Task<string> ExchangeCodeAsync(
        string code,
        string deviceId,
        ExternalAuthChallenge challenge,
        CancellationToken ct
    )
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = challenge.CodeVerifier,
            ["client_id"] = _config.ClientId!,
            ["device_id"] = deviceId,
            ["redirect_uri"] = challenge.RedirectUri,
            ["state"] = challenge.State,
        };

        if (!string.IsNullOrWhiteSpace(_config.ServiceToken))
        {
            form["service_token"] = _config.ServiceToken;
        }

        var http = _httpClientFactory.CreateClient(ExternalAuthHttpClient.Name);

        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(new Uri(_tokenEndpoint), content, ct);

        var json = await response.Content.ReadAsStringAsync(ct);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!response.IsSuccessStatusCode || root.TryGetProperty("error", out _))
        {
            throw new ExternalAuthProviderException(
                $"VK ID отклонил код авторизации: {ReadString(root, "error") ?? response.StatusCode.ToString()}."
            );
        }

        return ReadString(root, "access_token")
            ?? throw new ExternalAuthProviderException("VK ID не вернул токен доступа.");
    }

    private async Task<ExternalUserProfile> FetchProfileAsync(string accessToken, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["access_token"] = accessToken,
            ["client_id"] = _config.ClientId!,
        };

        var http = _httpClientFactory.CreateClient(ExternalAuthHttpClient.Name);

        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(new Uri(_userInfoEndpoint), content, ct);

        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalAuthProviderException(
                $"Не удалось получить данные аккаунта VK ID: {response.StatusCode}."
            );
        }

        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("user", out var user))
        {
            throw new ExternalAuthProviderException("VK ID не вернул данные пользователя.");
        }

        var subjectId = ReadString(user, "user_id");

        if (string.IsNullOrEmpty(subjectId))
        {
            throw new ExternalAuthProviderException("VK ID не вернул идентификатор пользователя.");
        }

        var firstName = ReadString(user, "first_name");
        var lastName = ReadString(user, "last_name");

        return new ExternalUserProfile
        {
            SubjectId = subjectId,
            Email = ReadString(user, "email"),
            // VK only exposes an address once the user has confirmed it.
            EmailIsVerified = true,
            DisplayName = string.Join(' ', new[] { firstName, lastName }.Where(part => part is not null)),
            FirstName = firstName,
            LastName = lastName,
            AvatarUrl = ReadString(user, "avatar"),
        };
    }

    /// <summary>VK returns user_id as a JSON number in some responses and a string in others.</summary>
    private static string? ReadString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() is { Length: > 0 } text ? text : null,
            JsonValueKind.Number => value.ToString(),
            _ => null,
        };
    }
}
