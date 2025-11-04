using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Noo.Api.Core.Response;
using Noo.Api.Sessions.DTO;

namespace Noo.IntegrationTests.Endpoints;

public class SessionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SessionTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "GET /session returns current user's sessions")]
    public async Task Get_Sessions_AsAuthenticated_ReturnsOk()
    {
        using var client = _factory.CreateClient().AsStudent();

        var resp = await client.GetAsync("/session");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IEnumerable<SessionDTO>>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
    }

    [Fact(DisplayName = "GET /session without auth returns 401 Unauthorized")]
    public async Task Get_Sessions_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient(); // no auth header
        var resp = await client.GetAsync("/session");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DELETE /session deletes current session (204)")]
    public async Task Delete_Current_Session_NoContent()
    {
        using var client = _factory.CreateClient().AsStudent();
        var resp = await client.DeleteAsync("/session");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "DELETE /session/{id} deleting another user's session returns 404 (not found for non-owner)")]
    public async Task Delete_Others_Session_NotFound()
    {
        var userId = TestDataHelpers.GetUserId(_factory, "student");
        var sessionId = await TestDataHelpers.CreateSessionAsync(_factory, userId);

        using var client = _factory.CreateClient().AsTeacher();
        var resp = await client.DeleteAsync($"/session/{sessionId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /session/{id} admin cannot delete another user's session (404)")]
    public async Task Admin_Cannot_Delete_Others_Session()
    {
        var userId = TestDataHelpers.GetUserId(_factory, "student");
        var sessionId = await TestDataHelpers.CreateSessionAsync(_factory, userId);

        using var client = _factory.CreateClient().AsAdmin();
        var resp = await client.DeleteAsync($"/session/{sessionId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /session/{id} with invalid id returns 400")]
    public async Task Delete_Session_InvalidId_BadRequest()
    {
        using var client = _factory.CreateClient().AsStudent();

        var resp = await client.DeleteAsync("/session/not-a-valid-ulid");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "DELETE /session/{id} for non-existing id returns 404")]
    public async Task Delete_Session_NonExisting_NotFound()
    {
        using var client = _factory.CreateClient().AsStudent();

        var missing = Ulid.NewUlid();
        var resp = await client.DeleteAsync($"/session/{missing}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /session/{id} deleting own session by id returns 204")]
    public async Task Delete_Own_Session_ById_NoContent()
    {
        var userId = TestDataHelpers.GetUserId(_factory, "student");
        var sessionId = await TestDataHelpers.CreateSessionAsync(_factory, userId);

        using var client = _factory.CreateClient().AsUserId(userId);
        var resp = await client.DeleteAsync($"/session/{sessionId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

