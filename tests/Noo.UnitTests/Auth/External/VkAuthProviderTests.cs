using System.Net;
using System.Web;
using Microsoft.Extensions.Options;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Providers.Config;
using Noo.Api.Auth.External.Types;

namespace Noo.UnitTests.Auth.External;

public class VkAuthProviderTests
{
    private static readonly ExternalAuthChallenge _challenge = new()
    {
        Provider = ExternalAuthProviderType.Vk,
        Intent = ExternalAuthIntent.Login,
        State = "state-value",
        CodeVerifier = "verifier-value",
        RedirectUri = "http://localhost:5189/auth/callback/vk",
    };

    private static VkAuthProvider CreateProvider(
        StubHttpMessageHandler handler,
        VkAuthConfig? config = null
    )
    {
        config ??= new VkAuthConfig { Enabled = true, ClientId = "client-id" };

        return new VkAuthProvider(handler.AsFactory(), Options.Create(config));
    }

    private static ExternalAuthCallback Callback(params (string Key, string Value)[] parameters)
    {
        return new ExternalAuthCallback(parameters.ToDictionary(p => p.Key, p => p.Value));
    }

    [Fact]
    public void IsEnabled_Needs_Only_A_ClientId_Because_Vk_Has_No_Secret()
    {
        var provider = CreateProvider(
            new StubHttpMessageHandler(),
            new VkAuthConfig { Enabled = true, ClientId = "client-id" }
        );

        Assert.True(provider.IsEnabled);
    }

    [Fact]
    public void BuildAuthorizationUrl_Sends_Space_Separated_Scopes_And_Pkce()
    {
        var provider = CreateProvider(new StubHttpMessageHandler());

        var url = provider.BuildAuthorizationUrl(_challenge, "challenge-value");

        var query = HttpUtility.ParseQueryString(new Uri(url).Query);

        Assert.StartsWith("https://id.vk.ru/authorize?", url);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("vkid.personal_info email", query["scope"]);
        Assert.Equal("challenge-value", query["code_challenge"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(_challenge.RedirectUri, query["redirect_uri"]);
    }

    [Fact]
    public async Task ResolveProfileAsync_Forwards_DeviceId_And_Verifier_But_No_Secret()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"user":{"user_id":"7"}}""");

        await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code"), ("device_id", "device-42")),
            _challenge
        );

        var tokenBody = HttpUtility.ParseQueryString(handler.Bodies[0]);

        Assert.Equal("https://id.vk.ru/oauth2/auth", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("authorization_code", tokenBody["grant_type"]);
        Assert.Equal("device-42", tokenBody["device_id"]);
        Assert.Equal("verifier-value", tokenBody["code_verifier"]);
        Assert.Equal("state-value", tokenBody["state"]);
        Assert.Null(tokenBody["client_secret"]);
        Assert.Null(tokenBody["service_token"]);
    }

    [Fact]
    public async Task ResolveProfileAsync_Includes_The_Service_Token_For_Confidential_Apps()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"user":{"user_id":"7"}}""");

        var config = new VkAuthConfig
        {
            Enabled = true,
            ClientId = "client-id",
            ServiceToken = "service-token",
        };

        await CreateProvider(handler, config).ResolveProfileAsync(
            Callback(("code", "the-code"), ("device_id", "device-42")),
            _challenge
        );

        Assert.Equal("service-token", HttpUtility.ParseQueryString(handler.Bodies[0])["service_token"]);
    }

    [Fact]
    public async Task ResolveProfileAsync_Throws_Without_A_DeviceId()
    {
        var provider = CreateProvider(new StubHttpMessageHandler());

        var exception = await Assert.ThrowsAsync<ExternalAuthCallbackParameterException>(
            () => provider.ResolveProfileAsync(Callback(("code", "the-code")), _challenge)
        );

        Assert.Contains("device_id", exception.Message);
    }

    [Fact]
    public async Task ResolveProfileAsync_Unwraps_The_User_Envelope()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue(
                """
                {
                  "user": {
                    "user_id": "98765",
                    "first_name": "Иван",
                    "last_name": "Петров",
                    "email": "ivan@vk.com",
                    "avatar": "https://vk.com/avatar.jpg"
                  }
                }
                """
            );

        var profile = await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code"), ("device_id", "device-42")),
            _challenge
        );

        Assert.Equal("98765", profile.SubjectId);
        Assert.Equal("ivan@vk.com", profile.Email);
        Assert.Equal("Иван Петров", profile.DisplayName);
        Assert.Equal("https://vk.com/avatar.jpg", profile.AvatarUrl);
    }

    [Fact]
    public async Task ResolveProfileAsync_Accepts_A_Numeric_UserId()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"user":{"user_id":98765,"first_name":"Иван"}}""");

        var profile = await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code"), ("device_id", "device-42")),
            _challenge
        );

        Assert.Equal("98765", profile.SubjectId);
    }

    [Fact]
    public async Task ResolveProfileAsync_Tolerates_A_Declined_Email_Scope()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"user":{"user_id":"7","first_name":"Иван"}}""");

        var profile = await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code"), ("device_id", "device-42")),
            _challenge
        );

        Assert.Null(profile.Email);
    }

    [Fact]
    public async Task ResolveProfileAsync_Throws_On_An_Error_Body_Despite_A_200()
    {
        var handler = new StubHttpMessageHandler().Enqueue("""{"error":"invalid_grant"}""");

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<ExternalAuthProviderException>(
            () =>
                provider.ResolveProfileAsync(
                    Callback(("code", "the-code"), ("device_id", "device-42")),
                    _challenge
                )
        );
    }

    [Fact]
    public async Task ResolveProfileAsync_Throws_When_The_Envelope_Is_Missing()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"user_id":"7"}""", HttpStatusCode.OK);

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<ExternalAuthProviderException>(
            () =>
                provider.ResolveProfileAsync(
                    Callback(("code", "the-code"), ("device_id", "device-42")),
                    _challenge
                )
        );
    }
}
