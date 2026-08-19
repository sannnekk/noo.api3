using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Noo.Api.Core.Security.Authorization;

namespace Noo.IntegrationTests.Endpoints;

public class AssignedWorkAccessTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AssignedWorkAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private Task<Ulid> SeedWorkAsync()
    {
        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");

        return TestDataHelpers.CreateAssignedWorkAsync(_factory, studentId, mentorId);
    }

    [Fact(DisplayName = "GET /assigned-work/{id} is not readable by a student who is not on the work")]
    public async Task Get_By_An_Outsider_Is_Not_Found()
    {
        using var client = _factory.CreateClient();
        var id = await SeedWorkAsync();

        var response = await client.AsUserId(Ulid.NewUlid()).GetAsync($"/assigned-work/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /assigned-work/{id} is readable by the student it belongs to")]
    public async Task Get_By_Its_Student_Succeeds()
    {
        using var client = _factory.CreateClient();
        var id = await SeedWorkAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        var response = await client.AsUserId(studentId).GetAsync($"/assigned-work/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /assigned-work/{id} is readable by staff who are on no work")]
    public async Task Get_By_Staff_Succeeds()
    {
        using var client = _factory.CreateClient();
        var id = await SeedWorkAsync();

        var response = await client.AsTeacher().GetAsync($"/assigned-work/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory(DisplayName = "the endpoints whose policies were never registered now answer")]
    [InlineData("archive")]
    [InlineData("unarchive")]
    public async Task Archive_Endpoints_Answer(string action)
    {
        using var client = _factory.CreateClient();
        var id = await SeedWorkAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        var response = await client
            .AsUserId(studentId)
            .PatchAsync($"/assigned-work/{id}/{action}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PATCH /assigned-work/{id}/replace-main-mentor is staff's to make")]
    public async Task Replace_Main_Mentor_Answers_For_Staff()
    {
        using var client = _factory.CreateClient();
        var id = await SeedWorkAsync();
        var username = $"mentor-{Guid.NewGuid():N}";
        var newMentorId = await TestDataHelpers.CreateUserAsync(
            _factory,
            username,
            $"{username}@example.com",
            "p4ssw0rd",
            UserRoles.Mentor
        );

        var response = await client
            .AsTeacher()
            .PatchAsJsonAsync(
                $"/assigned-work/{id}/replace-main-mentor",
                new { mentorId = newMentorId.ToString() }
            );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
