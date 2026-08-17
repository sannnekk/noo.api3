using System.Net;
using System.Web;
using Microsoft.Extensions.Options;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Providers.Config;
using Noo.Api.Auth.External.Types;

namespace Noo.UnitTests.Auth.External;

public class YandexAuthProviderTests
{
    private static readonly ExternalAuthChallenge _challenge = new()
    {
        Provider = ExternalAuthProviderType.Yandex,
        Intent = ExternalAuthIntent.Login,
        State = "state-value",
        CodeVerifier = "verifier-value",
        RedirectUri = "http://localhost:5189/auth/callback/yandex",
    };

    private static YandexAuthProvider CreateProvider(
        StubHttpMessageHandler handler,
        YandexAuthConfig? config = null
    )
    {
        config ??= new YandexAuthConfig
        {
            Enabled = true,
            ClientId = "client-id",
            ClientSecret = "client-secret",
        };

        return new YandexAuthProvider(handler.AsFactory(), Options.Create(config));
    }

    private static ExternalAuthCallback Callback(params (string Key, string Value)[] parameters)
    {
        return new ExternalAuthCallback(parameters.ToDictionary(p => p.Key, p => p.Value));
    }

    [Fact]
    public void IsEnabled_Is_False_Without_Credentials()
    {
        var provider = CreateProvider(
            new StubHttpMessageHandler(),
            new YandexAuthConfig { Enabled = true, ClientId = null, ClientSecret = null }
        );

        Assert.False(provider.IsEnabled);
    }

    [Fact]
    public void BuildAuthorizationUrl_Sends_Comma_Separated_Scopes_And_Pkce()
    {
        var provider = CreateProvider(new StubHttpMessageHandler());

        var url = provider.BuildAuthorizationUrl(_challenge, "challenge-value");

        var query = HttpUtility.ParseQueryString(new Uri(url).Query);

        Assert.StartsWith("https://oauth.yandex.ru/authorize?", url);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("client-id", query["client_id"]);
        Assert.Equal(_challenge.RedirectUri, query["redirect_uri"]);
        Assert.Equal("state-value", query["state"]);
        Assert.Equal("login:info,login:email,login:avatar", query["scope"]);
        Assert.Equal("challenge-value", query["code_challenge"]);
        Assert.Equal("S256", query["code_challenge_method"]);
    }

    [Fact]
    public async Task ResolveProfileAsync_Sends_The_Verifier_And_Secret_To_The_Token_Endpoint()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"id":"1","login":"ivan"}""");

        await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code")),
            _challenge
        );

        var tokenBody = HttpUtility.ParseQueryString(handler.Bodies[0]);

        Assert.Equal("https://oauth.yandex.ru/token", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("authorization_code", tokenBody["grant_type"]);
        Assert.Equal("the-code", tokenBody["code"]);
        Assert.Equal("verifier-value", tokenBody["code_verifier"]);
        Assert.Equal("client-secret", tokenBody["client_secret"]);
        Assert.Equal(_challenge.RedirectUri, tokenBody["redirect_uri"]);
    }

    [Fact]
    public async Task ResolveProfileAsync_Sends_The_Access_Token_As_An_OAuth_Header()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"id":"1"}""");

        await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code")),
            _challenge
        );

        var authorization = handler.Requests[1].Headers.Authorization;

        Assert.Equal("OAuth", authorization!.Scheme);
        Assert.Equal("token-value", authorization.Parameter);
    }

    [Fact]
    public async Task ResolveProfileAsync_Maps_The_Full_Profile()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue(
                """
                {
                  "id": "12345",
                  "login": "ivan.petrov",
                  "real_name": "Иван Петров",
                  "first_name": "Иван",
                  "last_name": "Петров",
                  "default_email": "ivan@yandex.ru",
                  "default_avatar_id": "abc123",
                  "is_avatar_empty": false
                }
                """
            );

        var profile = await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code")),
            _challenge
        );

        Assert.Equal("12345", profile.SubjectId);
        Assert.Equal("ivan@yandex.ru", profile.Email);
        Assert.True(profile.EmailIsVerified);
        Assert.Equal("Иван Петров", profile.DisplayName);
        Assert.Equal("Иван", profile.FirstName);
        Assert.Equal("Петров", profile.LastName);
        Assert.Equal("ivan.petrov", profile.ProviderLogin);
        Assert.Equal(
            "https://avatars.yandex.net/get-yapic/abc123/islands-200",
            profile.AvatarUrl
        );
    }

    [Fact]
    public async Task ResolveProfileAsync_Skips_An_Empty_Avatar()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"id":"1","default_avatar_id":"abc","is_avatar_empty":true}""");

        var profile = await CreateProvider(handler).ResolveProfileAsync(
            Callback(("code", "the-code")),
            _challenge
        );

        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public async Task ResolveProfileAsync_Throws_Without_A_Code()
    {
        var provider = CreateProvider(new StubHttpMessageHandler());

        await Assert.ThrowsAsync<ExternalAuthCallbackParameterException>(
            () => provider.ResolveProfileAsync(Callback(("state", "s")), _challenge)
        );
    }

    [Fact]
    public async Task ResolveProfileAsync_Throws_When_The_Exchange_Fails()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest);

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<ExternalAuthProviderException>(
            () => provider.ResolveProfileAsync(Callback(("code", "the-code")), _challenge)
        );
    }

    [Fact]
    public async Task ResolveProfileAsync_Throws_When_The_Profile_Has_No_Id()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue("""{"access_token":"token-value"}""")
            .Enqueue("""{"login":"ivan"}""");

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<ExternalAuthProviderException>(
            () => provider.ResolveProfileAsync(Callback(("code", "the-code")), _challenge)
        );
    }
}
