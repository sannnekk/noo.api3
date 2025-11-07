using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Types;
using Xunit.Abstractions;

namespace Noo.IntegrationTests.Endpoints;

public class WorkTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITestOutputHelper _outputHelper;

    public WorkTests(ApiFactory factory, ITestOutputHelper outputHelper)
    {
        _factory = factory;
        _outputHelper = outputHelper;
    }

    private static async Task<Ulid> CreateSubjectAsync(HttpClient client, string? name = null, string? color = null)
    {
        name ??= $"Subj-{Guid.NewGuid():N}";
        color ??= "#00AAFF";

        var resp = await client.AsAdmin()
            .PostAsJsonAsync("/subject", new SubjectCreationDTO { Name = name, Color = color }, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Data.Should().NotBeNull();
        return payload!.Data!.Id;
    }

    private static object BuildValidCreateWorkPayload(Ulid subjectId, string? title = null, WorkType type = WorkType.Test)
    {
        title ??= $"Work-{Guid.NewGuid():N}";

        return new
        {
            title,
            type = type.ToString(),
            subjectId = subjectId.ToString(),
            tasks = new[]
            {
                new
                {
                    type = WorkTaskType.Word.ToString(),
                    order = 0,
                    maxScore = 1,
                    content = RichTextFactory.Create("Question 1")
                }
            }
        };
    }

    private async Task<Ulid> CreateWorkAsync(HttpClient client, Ulid subjectId, string? title = null, WorkType type = WorkType.Test)
    {
        var createPayload = BuildValidCreateWorkPayload(subjectId, title, type);

        var resp = await client.AsTeacher().PostAsJsonAsync("/work", createPayload, JsonOptions);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        result.Should().NotBeNull();
        return result!.Data!.Id;
    }

    [Fact(DisplayName = "GET /work as teacher returns 200 OK")]
    public async Task Search_Works_AsTeacher_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var resp = await client.AsTeacher().GetAsync("/work");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("data");
        json.Should().Contain("meta");
    }

    [Fact(DisplayName = "GET /work as student returns 403 Forbidden")]
    public async Task Search_Works_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var resp = await client.AsStudent().GetAsync("/work");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /work without auth returns 401 Unauthorized")]
    public async Task Search_Works_WithoutAuth_Unauthorized()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/work");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /work as teacher returns 201 Created then GET by id returns 200 OK")]
    public async Task Create_Work_AsTeacher_ReturnsCreated_Then_GetById_Ok()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);

        var workId = await CreateWorkAsync(client, subjectId, title: "Initial Title", type: WorkType.Test);
        var getResp = await client.AsTeacher().GetAsync($"/work/{workId}");

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var idInPayload = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
        idInPayload.Should().Be(workId.ToString());
        // Ensure tasks are included
        doc.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact(DisplayName = "GET /work/{id} without auth returns 401 Unauthorized")]
    public async Task Get_Work_ById_WithoutAuth_Unauthorized()
    {
        // Use separate authenticated clients for setup to avoid leaking auth headers
        using var adminClient = _factory.CreateClient().AsAdmin();
        var subjectId = await CreateSubjectAsync(adminClient);
        using var teacherClient = _factory.CreateClient().AsTeacher();
        var workId = await CreateWorkAsync(teacherClient, subjectId);

        // Anonymous client for assertion
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync($"/work/{workId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /work/{id} as student returns 403 Forbidden")]
    public async Task Get_Work_ById_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId);
        var resp = await client.AsStudent().GetAsync($"/work/{workId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /work as student returns 403 Forbidden")]
    public async Task Create_Work_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var payload = BuildValidCreateWorkPayload(subjectId);
        var resp = await client.AsStudent().PostAsJsonAsync("/work", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /work without auth returns 401 Unauthorized")]
    public async Task Create_Work_WithoutAuth_Unauthorized()
    {
        // Setup with authenticated admin client
        using var adminClient = _factory.CreateClient().AsAdmin();
        var subjectId = await CreateSubjectAsync(adminClient);
        var payload = BuildValidCreateWorkPayload(subjectId);
        // Anonymous client for assertion
        using var anonClient = _factory.CreateClient();
        var resp = await anonClient.PostAsJsonAsync("/work", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /work with invalid payload returns 400 Bad Request")]
    public async Task Create_Work_InvalidPayload_BadRequest()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);

        // Invalid: empty title and tasks empty
        var badPayload = new
        {
            title = "",
            type = WorkType.Test.ToString(),
            subjectId = subjectId.ToString(),
            tasks = Array.Empty<object>()
        };
        var resp = await client.AsTeacher().PostAsJsonAsync("/work", badPayload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /work/{id} for non-existing id returns 404 Not Found")]
    public async Task Get_Work_NotFound()
    {
        using var client = _factory.CreateClient();
        var resp = await client.AsTeacher().GetAsync($"/work/{Ulid.NewUlid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PATCH /work updates title returns 204 No Content and persists")]
    public async Task Patch_Work_UpdateTitle_NoContent_Then_Persists()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId, title: "Before");

        const string patchJson = "[ { \"op\": \"replace\", \"path\": \"/title\", \"value\": \"After\" } ]";
        var patchResp = await client.AsTeacher().PatchAsync($"/work/{workId}", new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
        patchResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await client.AsTeacher().GetAsync($"/work/{workId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetProperty("title").GetString().Should().Be("After");
    }

    [Fact(DisplayName = "PATCH /work with invalid value returns 400 Bad Request")]
    public async Task Patch_Work_InvalidValue_BadRequest()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId);

        // maxScore must be >= 1, set to 0 -> validation error
        const string patchJson = "[ { \"op\": \"replace\", \"path\": \"/tasks/0/maxScore\", \"value\": 0 } ]";
        var resp = await client.AsTeacher().PatchAsync($"/work/{workId}", new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "PATCH /work as student returns 403 Forbidden")]
    public async Task Patch_Work_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId);
        const string patchJson = "[ { \"op\": \"replace\", \"path\": \"/title\", \"value\": \"X\" } ]";
        var resp = await client.AsStudent().PatchAsync($"/work/{workId}", new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PATCH /work/{id} non-existing returns 404 Not Found")]
    public async Task Patch_Work_NotFound()
    {
        using var client = _factory.CreateClient();
        const string patchJson = "[ { \"op\": \"replace\", \"path\": \"/title\", \"value\": \"T\" } ]";
        var resp = await client.AsTeacher().PatchAsync($"/work/{Ulid.NewUlid()}", new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PATCH /work without auth returns 401 Unauthorized")]
    public async Task Patch_Work_WithoutAuth_Unauthorized()
    {
        using var client = _factory.CreateClient();
        const string patchJson = "[ { \"op\": \"replace\", \"path\": \"/title\", \"value\": \"T\" } ]";
        var resp = await client.PatchAsync($"/work/{Ulid.NewUlid()}", new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DELETE /work returns 204 No Content then GET returns 404 Not Found")]
    public async Task Delete_Work_NoContent_And_After_Get_NotFound()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId);

        var delResp = await client.AsTeacher().DeleteAsync($"/work/{workId}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await client.AsTeacher().GetAsync($"/work/{workId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /work as student returns 403 Forbidden")]
    public async Task Delete_Work_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var delResp = await client.AsStudent().DeleteAsync($"/work/{Ulid.NewUlid()}");
        delResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DELETE /work without auth returns 401 Unauthorized")]
    public async Task Delete_Work_WithoutAuth_Unauthorized()
    {
        using var client = _factory.CreateClient();
        var delResp = await client.DeleteAsync($"/work/{Ulid.NewUlid()}");
        delResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /work filtered by type returns expected results")]
    public async Task Search_Filter_ByType_Works()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        await CreateWorkAsync(client, subjectId, title: "W1", type: WorkType.Test);
        await CreateWorkAsync(client, subjectId, title: "W2", type: WorkType.Phrase);

        var resp = await client.AsTeacher().GetAsync($"/work?Type={WorkType.Test}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(e => e.GetProperty("type").GetString() == nameof(WorkType.Test).ToLower());
    }
}

