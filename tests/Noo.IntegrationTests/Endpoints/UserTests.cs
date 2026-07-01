using System.Net;
using System.Text.Json;
using FluentAssertions;
using Noo.Api.Core.Security.Authorization;

namespace Noo.IntegrationTests.Endpoints;

public class UserTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UserTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Ulid id, string username)> SeedUserAsync(UserRoles role)
    {
        var username = $"u-{Guid.NewGuid():N}";
        var id = await TestDataHelpers.CreateUserAsync(
            _factory, username, $"{username}@example.com", "password", role);
        return (id, username);
    }

    private static async Task<List<(string username, string role)>> GetUsersAsync(HttpClient client, string url)
    {
        var resp = await client.AsAdmin().GetAsync(url);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(e => (
                e.GetProperty("username").GetString()!,
                e.GetProperty("role").GetString()!))
            .ToList();
    }

    [Fact(DisplayName = "GET /user filters by a single role")]
    public async Task Search_Users_SingleRole_Works()
    {
        using var client = _factory.CreateClient();
        var teacher = await SeedUserAsync(UserRoles.Teacher);
        await SeedUserAsync(UserRoles.Student);

        var users = await GetUsersAsync(client, "/user?role=teacher&perPage=100");

        users.Should().OnlyContain(u => u.role == "teacher");
        users.Should().Contain(u => u.username == teacher.username);
    }

    [Fact(DisplayName = "GET /user filters by multiple roles (IN)")]
    public async Task Search_Users_MultipleRoles_Works()
    {
        using var client = _factory.CreateClient();
        var mentor = await SeedUserAsync(UserRoles.Mentor);
        var assistant = await SeedUserAsync(UserRoles.Assistant);
        var student = await SeedUserAsync(UserRoles.Student);

        var users = await GetUsersAsync(client, "/user?role=mentor&role=assistant&perPage=100");

        users.Should().OnlyContain(u => u.role == "mentor" || u.role == "assistant");
        users.Should().Contain(u => u.username == mentor.username);
        users.Should().Contain(u => u.username == assistant.username);
        users.Should().NotContain(u => u.username == student.username);
    }
}
