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

    // The following tests pin the UserAccessRequirementHandler route contract: the
    // handler grants students self access only when the target id is exposed under
    // the "userId" route value. If an endpoint renames the parameter (studentId,
    // mentorId, ...), self access breaks and these tests start returning 403.

    [Fact(DisplayName = "GET /user/{userId} allows a student to read own data")]
    public async Task Get_User_Student_Can_Read_Own_Data()
    {
        using var client = _factory.CreateClient();
        var student = await SeedUserAsync(UserRoles.Student);

        var response = await client.AsUserId(student.id).GetAsync($"/user/{student.id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /user/{userId} forbids a student to read another user")]
    public async Task Get_User_Student_Cannot_Read_Other_User()
    {
        using var client = _factory.CreateClient();
        var student = await SeedUserAsync(UserRoles.Student);
        var otherStudent = await SeedUserAsync(UserRoles.Student);

        var response = await client.AsUserId(student.id).GetAsync($"/user/{otherStudent.id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /user/{userId}/mentor-assignment allows self access for a student")]
    public async Task Get_MentorAssignments_Student_Can_Read_Own()
    {
        using var client = _factory.CreateClient();
        var student = await SeedUserAsync(UserRoles.Student);

        var response = await client
            .AsUserId(student.id)
            .GetAsync($"/user/{student.id}/mentor-assignment");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /user/{userId}/mentor-assignment forbids access to another user's assignments")]
    public async Task Get_MentorAssignments_Student_Cannot_Read_Others()
    {
        using var client = _factory.CreateClient();
        var student = await SeedUserAsync(UserRoles.Student);
        var otherStudent = await SeedUserAsync(UserRoles.Student);

        var response = await client
            .AsUserId(student.id)
            .GetAsync($"/user/{otherStudent.id}/mentor-assignment");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /user/{userId}/student-assignment allows self access")]
    public async Task Get_StudentAssignments_Self_Access_Works()
    {
        using var client = _factory.CreateClient();
        var student = await SeedUserAsync(UserRoles.Student);

        var response = await client
            .AsUserId(student.id)
            .GetAsync($"/user/{student.id}/student-assignment");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
