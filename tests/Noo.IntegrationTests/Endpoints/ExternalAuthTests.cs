using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Auth;
using Noo.Api.Auth.External.DTO;
using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Users.Models;

namespace Noo.IntegrationTests.Endpoints;

public class ExternalAuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    // Mirror the API's serialization so responses (hyphen-lowercase enums, Moscow dates) round-trip.
    private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

    public ExternalAuthTests(ApiFactory factory)
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

    private static string UniqueSubject(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    /// <summary>Starts an attempt and pulls the state back out of the provider URL.</summary>
    private static async Task<string> StartAsync(HttpClient client, string path, string? returnUrl = null)
    {
        var response = await client.PostAsJsonAsync(path, new StartExternalAuthDTO { ReturnUrl = returnUrl });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponseDTO<ExternalAuthUrlDTO>>(JsonOptions);
        var query = HttpUtility.ParseQueryString(new Uri(payload!.Data!.Url).Query);

        return query["state"]!;
    }

    private static Task<HttpResponseMessage> CallbackAsync(
        HttpClient client, string provider, string state, string code)
        => client.PostAsJsonAsync(
            $"/auth/external/{provider}/callback",
            new ExternalAuthCallbackDTO { Parameters = new() { ["state"] = state, ["code"] = code } });

    private UserModel? FindUserById(Ulid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        return db.GetDbSet<UserModel>().FirstOrDefault(user => user.Id == id);
    }

    private List<UserIdentityModel> FindIdentities(Ulid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        return db.GetDbSet<UserIdentityModel>().Where(identity => identity.UserId == userId).ToList();
    }

    [Fact(DisplayName = "GET /auth/external/providers lists the configured providers")]
    public async Task GetProviders_ReturnsEnabledProviders()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/auth/external/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponseDTO<IEnumerable<ExternalAuthProviderDTO>>>(JsonOptions);

        payload!.Data.Should().NotBeNull();
        payload.Data!.Select(provider => provider.Provider)
            .Should().BeEquivalentTo([ExternalAuthProviderType.Yandex, ExternalAuthProviderType.Vk]);
    }

    [Fact(DisplayName = "An unknown identity creates a verified account and opens a session")]
    public async Task Callback_UnknownIdentity_CreatesVerifiedUserAndSession()
    {
        var subject = UniqueSubject("newcomer");

        using var client = _factory.CreateClient();
        var state = await StartAsync(client, "/auth/external/yandex/start", "/courses");
        var response = await CallbackAsync(client, "yandex", state, $"{subject}|{subject}@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions);

        payload!.Data!.Intent.Should().Be(ExternalAuthIntent.Login);
        payload.Data.ReturnUrl.Should().Be("/courses");
        payload.Data.Session.Should().NotBeNull();
        payload.Data.Session!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var user = FindUserById(payload.Data.Session.UserId);
        user.Should().NotBeNull();
        user!.IsVerified.Should().BeTrue();
        user.PasswordHash.Should().BeNull();
        user.Username.Should().MatchRegex("^[a-zA-Z0-9_-]{3,20}$");

        FindIdentities(user.Id).Should().ContainSingle(identity => identity.SubjectId == subject);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(cookie => cookie.Contains(RefreshCookie.Name));
    }

    [Fact(DisplayName = "A second login through the same identity reuses the account")]
    public async Task Callback_KnownIdentity_ReusesTheSameUser()
    {
        var subject = UniqueSubject("returning");
        var code = $"{subject}|{subject}@example.com";

        using var client = _factory.CreateClient();

        var first = await CallbackAsync(
            client, "yandex", await StartAsync(client, "/auth/external/yandex/start"), code);
        var second = await CallbackAsync(
            client, "yandex", await StartAsync(client, "/auth/external/yandex/start"), code);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstUserId = (await first.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions))!.Data!.Session!.UserId;
        var secondUserId = (await second.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions))!.Data!.Session!.UserId;

        secondUserId.Should().Be(firstUserId);
        FindIdentities(firstUserId).Should().ContainSingle();
    }

    [Fact(DisplayName = "A VK account without an email still gets an account")]
    public async Task Callback_WithoutEmail_CreatesAccountWithNullEmail()
    {
        var subject = UniqueSubject("no-email");

        using var client = _factory.CreateClient();
        var state = await StartAsync(client, "/auth/external/vk/start");
        var response = await CallbackAsync(client, "vk", state, subject);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions);

        FindUserById(payload!.Data!.Session!.UserId)!.Email.Should().BeNull();
    }

    [Fact(DisplayName = "A matching email silently links an existing student account")]
    public async Task Callback_MatchingStudentEmail_LinksInsteadOfCreating()
    {
        var username = UniqueSubject("student-link");
        var email = $"{username}@example.com";
        var userId = await TestDataHelpers.CreateUserAsync(_factory, username, email, "Passw0rd!");

        using var client = _factory.CreateClient();
        var state = await StartAsync(client, "/auth/external/yandex/start");
        var response = await CallbackAsync(client, "yandex", state, $"{UniqueSubject("subject")}|{email}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions);

        payload!.Data!.Session!.UserId.Should().Be(userId);
        FindIdentities(userId).Should().ContainSingle();
    }

    [Fact(DisplayName = "A matching email never claims a privileged account")]
    public async Task Callback_MatchingAdminEmail_Returns409()
    {
        var username = UniqueSubject("admin-link");
        var email = $"{username}@example.com";
        var userId = await TestDataHelpers.CreateUserAsync(
            _factory, username, email, "Passw0rd!", UserRoles.Admin);

        using var client = _factory.CreateClient();
        var state = await StartAsync(client, "/auth/external/yandex/start");
        var response = await CallbackAsync(client, "yandex", state, $"{UniqueSubject("subject")}|{email}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        FindIdentities(userId).Should().BeEmpty();
    }

    [Fact(DisplayName = "A state is redeemable exactly once")]
    public async Task Callback_ReplayedState_Returns400()
    {
        var subject = UniqueSubject("replay");

        using var client = _factory.CreateClient();
        var state = await StartAsync(client, "/auth/external/yandex/start");

        (await CallbackAsync(client, "yandex", state, subject)).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await CallbackAsync(client, "yandex", state, subject)).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "A state issued for one provider is not accepted by another")]
    public async Task Callback_ProviderMismatch_Returns400()
    {
        using var client = _factory.CreateClient();
        var state = await StartAsync(client, "/auth/external/yandex/start");

        var response = await CallbackAsync(client, "vk", state, UniqueSubject("mismatch"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "An absolute return URL is rejected")]
    public async Task Start_AbsoluteReturnUrl_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/external/yandex/start",
            new StartExternalAuthDTO { ReturnUrl = "https://evil.test/steal" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Linking attaches the provider to the caller's own account")]
    public async Task LinkFlow_AttachesAndDetachesIdentity()
    {
        var username = UniqueSubject("linker");
        var userId = await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient().AsUserId(userId);

        var state = await StartAsync(
            client, "/auth/external/vk/link/start", "/settings/connected-accounts");
        var response = await CallbackAsync(client, "vk", state, UniqueSubject("linked-subject"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions);

        payload!.Data!.Intent.Should().Be(ExternalAuthIntent.Link);
        payload.Data.Session.Should().BeNull();
        payload.Data.ReturnUrl.Should().Be("/settings/connected-accounts");

        var identities = await client.GetFromJsonAsync<ApiResponseDTO<IEnumerable<LinkedIdentityDTO>>>(
            "/auth/external/identities", JsonOptions);

        identities!.Data!.Should().ContainSingle(identity =>
            identity.Provider == ExternalAuthProviderType.Vk);

        var unlink = await client.DeleteAsync("/auth/external/identities/vk");

        unlink.StatusCode.Should().Be(HttpStatusCode.NoContent);
        FindIdentities(userId).Should().BeEmpty();
    }

    [Fact(DisplayName = "Unlinking a provider that was never linked returns 404")]
    public async Task Unlink_NotLinked_Returns404()
    {
        var username = UniqueSubject("unlinked");
        var userId = await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "Passw0rd!");

        using var client = _factory.CreateClient().AsUserId(userId);

        var response = await client.DeleteAsync("/auth/external/identities/yandex");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Unlinking the only way into a password-less account returns 409")]
    public async Task Unlink_LastCredential_Returns409()
    {
        using var anonymous = _factory.CreateClient();
        var state = await StartAsync(anonymous, "/auth/external/yandex/start");
        var login = await CallbackAsync(anonymous, "yandex", state, UniqueSubject("locked-out"));

        var payload = await login.Content
            .ReadFromJsonAsync<ApiResponseDTO<ExternalAuthResultDTO>>(JsonOptions);
        var userId = payload!.Data!.Session!.UserId;

        using var client = _factory.CreateClient().AsUserId(userId);

        var response = await client.DeleteAsync("/auth/external/identities/yandex");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        FindIdentities(userId).Should().ContainSingle();
    }

    [Fact(DisplayName = "Identity management requires an authenticated caller")]
    public async Task Identities_Anonymous_Returns401()
    {
        using var client = _factory.CreateClient();

        (await client.GetAsync("/auth/external/identities")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/auth/external/identities/yandex")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/auth/external/yandex/link/start", new StartExternalAuthDTO()))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
