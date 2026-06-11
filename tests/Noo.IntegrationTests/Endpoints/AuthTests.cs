using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Noo.Api.Auth;
using Noo.Api.Auth.DTO;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Json;

namespace Noo.IntegrationTests.Endpoints;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    // Mirror the API's serialization so responses (hyphen-lowercase enums, Moscow dates) round-trip.
    private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new HyphenLowerCaseStringEnumConverterFactory());
        options.Converters.Add(new MoscowDateTimeConverter());
        options.Converters.Add(new MoscowNullableDateTimeConverter());
        return options;
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact(DisplayName = "POST /auth/login with valid credentials returns 200 and tokens")]
    public async Task Login_ValidCredentials_ReturnsOkAndSetsRefreshCookie()
    {
        var username = UniqueName("login-ok");
        var userId = await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new LoginDTO
        {
            UsernameOrEmail = username,
            Password = "Passw0rd!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<LoginResponseDTO>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        payload.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.Data.UserId.Should().Be(userId);
        payload.Data.UserRole.Should().Be(UserRoles.Student);

        resp.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains(RefreshCookie.Name));
    }

    [Fact(DisplayName = "POST /auth/login with wrong password returns 401")]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var username = UniqueName("login-badpwd");
        await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new LoginDTO
        {
            UsernameOrEmail = username,
            Password = "WrongPass1!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /auth/login for unknown user returns 401")]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new LoginDTO
        {
            UsernameOrEmail = UniqueName("ghost"),
            Password = "Passw0rd!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /auth/login for unverified user returns 401")]
    public async Task Login_UnverifiedUser_ReturnsUnauthorized()
    {
        var username = UniqueName("login-unverified");
        await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!", isVerified: false);

        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new LoginDTO
        {
            UsernameOrEmail = username,
            Password = "Passw0rd!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /auth/login for blocked user returns 403")]
    public async Task Login_BlockedUser_ReturnsForbidden()
    {
        var username = UniqueName("login-blocked");
        await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!", isBlocked: true);

        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new LoginDTO
        {
            UsernameOrEmail = username,
            Password = "Passw0rd!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /auth/login with missing fields returns 400")]
    public async Task Login_MissingFields_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/login", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST /auth/register creates an unverified user and returns 204")]
    public async Task Register_NewUser_CreatesUnverifiedUser()
    {
        var username = UniqueName("reg");
        using var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/auth/register", new RegisterDTO
        {
            Name = "New User",
            Username = username,
            Email = $"{username}@example.com",
            Password = "Passw0rd!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var created = TestDataHelpers.FindUser(_factory, username);
        created.Should().NotBeNull();
        created!.IsVerified.Should().BeFalse();
        created.Role.Should().Be(UserRoles.Student);
    }

    [Fact(DisplayName = "POST /auth/register with duplicate username returns 409")]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        var username = UniqueName("reg-dup");
        await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/auth/register", new RegisterDTO
        {
            Name = "Dup User",
            Username = username,
            Email = $"{UniqueName("other")}@example.com",
            Password = "Passw0rd!"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "POST /auth/register with weak password returns 400")]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var username = UniqueName("reg-weak");
        using var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/auth/register", new RegisterDTO
        {
            Name = "Weak User",
            Username = username,
            Email = $"{username}@example.com",
            Password = "weak"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /auth/username-check returns false for a taken username")]
    public async Task UsernameCheck_Taken_ReturnsFalse()
    {
        var username = UniqueName("taken");
        await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/auth/username-check/{username}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<bool>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().BeFalse();
    }

    [Fact(DisplayName = "GET /auth/username-check returns true for a free username")]
    public async Task UsernameCheck_Free_ReturnsTrue()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/auth/username-check/{UniqueName("free")}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<bool>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().BeTrue();
    }

    [Fact(DisplayName = "PATCH /auth/request-password-change for an existing user returns 204")]
    public async Task RequestPasswordChange_ExistingUser_ReturnsNoContent()
    {
        var username = UniqueName("pwd-req");
        var email = $"{username}@example.com";
        await TestDataHelpers.CreateUserAsync(_factory, username, email, "Passw0rd!");

        using var client = _factory.CreateClient();
        var resp = await client.PatchAsJsonAsync("/auth/request-password-change",
            new RequestPasswordChangeDTO { Email = email });

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PATCH /auth/request-password-change for unknown email returns 404")]
    public async Task RequestPasswordChange_UnknownEmail_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PatchAsJsonAsync("/auth/request-password-change",
            new RequestPasswordChangeDTO { Email = $"{UniqueName("nobody")}@example.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PATCH /auth/confirm-password-change with an invalid token returns 401")]
    public async Task ConfirmPasswordChange_InvalidToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PatchAsJsonAsync("/auth/confirm-password-change",
            new ConfirmPasswordChangeDTO { Token = "not-a-real-token", NewPassword = "Passw0rd!" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /auth/refresh without a refresh cookie returns 401")]
    public async Task Refresh_NoCookie_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsync("/auth/refresh", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /auth/refresh after login rotates the token and returns 200")]
    public async Task Refresh_AfterLogin_ReturnsOk()
    {
        var username = UniqueName("refresh");
        await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new LoginDTO
        {
            UsernameOrEmail = username,
            Password = "Passw0rd!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        // The refresh cookie is marked Secure, so the http test client won't auto-resend it.
        // Forward it explicitly to exercise the rotation path.
        var refreshCookie = ExtractCookie(login, RefreshCookie.Name);
        refreshCookie.Should().NotBeNull();

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{RefreshCookie.Name}={refreshCookie}");
        var refresh = await client.SendAsync(refreshRequest);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await refresh.Content.ReadFromJsonAsync<ApiResponseDTO<LoginResponseDTO>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    private static string? ExtractCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = $"{name}=";
        var header = cookies.FirstOrDefault(c => c.StartsWith(prefix, StringComparison.Ordinal));
        if (header is null)
        {
            return null;
        }

        var value = header[prefix.Length..];
        var semicolon = value.IndexOf(';');
        return semicolon >= 0 ? value[..semicolon] : value;
    }
}
