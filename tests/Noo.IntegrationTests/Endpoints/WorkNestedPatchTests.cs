using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Integration coverage for PATCH /work/{id}, whose body is a
/// JSON Patch over <see cref="Noo.Api.Works.DTO.UpdateWorkDTO"/>. The Tasks
/// child collection is exposed to the patch as a dictionary keyed by task Id,
/// merged back into the EF-tracked entity via
/// <see cref="Noo.Api.Core.Utils.AutoMapper.NestedEntityMappingExtensions.MapDictionaryToCollection"/>.
///
/// These exercise the full controller -> JsonPatchUpdateService -> mapper ->
/// merge -> EF SaveChanges pipeline, with emphasis on the nested tasks dict,
/// the recomputed MaxScore aggregate, rich-text task fields, and combined
/// top-level + nested operations in a single document.
/// </summary>
public class WorkNestedPatchTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public WorkNestedPatchTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Ulid> CreateSubjectAsync(HttpClient client)
    {
        var resp = await client.AsAdmin()
            .PostAsJsonAsync("/subject", new SubjectCreationDTO
            {
                Name = $"Subj-{Guid.NewGuid():N}",
                Color = "#00AAFF",
            }, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        return body!.Data!.Id;
    }

    private async Task<Ulid> CreateWorkWithTaskAsync(HttpClient client, Ulid subjectId, int maxScore = 5)
    {
        var payload = new
        {
            title = $"Work-{Guid.NewGuid():N}",
            type = WorkType.Test.ToString(),
            subjectId = subjectId.ToString(),
            tasks = new[]
            {
                new
                {
                    type = WorkTaskType.Word.ToString(),
                    order = 0,
                    maxScore,
                    content = RichTextFactory.Create("Q1"),
                }
            }
        };
        var resp = await client.AsTeacher().PostAsJsonAsync("/work", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        return body!.Data!.Id;
    }

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, string body)
        => client.PatchAsync(path, new StringContent(body, Encoding.UTF8, "application/json-patch+json"));

    private async Task<JsonElement> GetWorkDataAsync(HttpClient client, Ulid workId)
    {
        var resp = await client.AsTeacher().GetAsync($"/work/{workId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
    }

    private async Task<string> GetSingleTaskIdAsync(HttpClient client, Ulid workId)
    {
        var data = await GetWorkDataAsync(client, workId);
        var tasks = data.GetProperty("tasks").EnumerateArray().ToList();
        tasks.Should().HaveCount(1);
        return tasks[0].GetProperty("id").GetString()!;
    }

    [Fact(DisplayName = "PATCH /work adds two tasks at once; existing task preserved and MaxScore recomputed")]
    public async Task Patch_Work_Add_Two_Tasks_Recomputes_MaxScore()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId, maxScore: 5);
        var existingTaskId = await GetSingleTaskIdAsync(client, workId);

        var t1 = Ulid.NewUlid().ToString();
        var t2 = Ulid.NewUlid().ToString();
        var patch = $$"""
            [
              { "op": "add", "path": "/tasks/{{t1}}", "value": {
                  "id": "{{t1}}", "type": "word", "order": 1, "maxScore": 3,
                  "content": {"$type":"delta","ops":[{"insert":"t1\n"}]} } },
              { "op": "add", "path": "/tasks/{{t2}}", "value": {
                  "id": "{{t2}}", "type": "word", "order": 2, "maxScore": 7,
                  "content": {"$type":"delta","ops":[{"insert":"t2\n"}]} } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetWorkDataAsync(client, workId);
        var tasks = data.GetProperty("tasks").EnumerateArray().ToList();
        tasks.Should().HaveCount(3);
        tasks.Select(t => t.GetProperty("id").GetString()).Should()
            .Contain(existingTaskId).And.Contain(t1).And.Contain(t2);
        data.GetProperty("maxScore").GetInt32().Should().Be(5 + 3 + 7);
    }

    [Fact(DisplayName = "PATCH /work replaces a nested task's rich-text content in place")]
    public async Task Patch_Work_Replace_Task_RichText_Content()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId);
        var taskId = await GetSingleTaskIdAsync(client, workId);

        var patch = $$"""
            [
              { "op": "replace", "path": "/tasks/{{taskId}}/content",
                "value": {"$type":"delta","ops":[{"insert":"updated question\n"}]} }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var task = await db.GetDbSet<WorkTaskModel>().AsNoTracking()
            .FirstAsync(t => t.Id == Ulid.Parse(taskId));
        task.Content!.ToString().Should().Contain("updated question");
    }

    [Fact(DisplayName = "PATCH /work updates several scalar fields of an existing task")]
    public async Task Patch_Work_Update_Task_Scalars()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId);
        var taskId = await GetSingleTaskIdAsync(client, workId);

        var patch = $$"""
            [
              { "op": "replace", "path": "/tasks/{{taskId}}/order", "value": 4 },
              { "op": "replace", "path": "/tasks/{{taskId}}/maxScore", "value": 8 },
              { "op": "replace", "path": "/tasks/{{taskId}}/checkStrategy", "value": "multiple-choice" }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetWorkDataAsync(client, workId);
        var task = data.GetProperty("tasks").EnumerateArray().Single();
        task.GetProperty("order").GetInt32().Should().Be(4);
        task.GetProperty("maxScore").GetInt32().Should().Be(8);
        data.GetProperty("maxScore").GetInt32().Should().Be(8);
    }

    [Fact(DisplayName = "PATCH /work combines a top-level title change and a nested task add")]
    public async Task Patch_Work_TopLevel_And_Nested_In_One_Document()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId, maxScore: 2);
        var existingTaskId = await GetSingleTaskIdAsync(client, workId);

        var newTaskId = Ulid.NewUlid().ToString();
        var patch = $$"""
            [
              { "op": "replace", "path": "/title", "value": "Combined Update" },
              { "op": "add", "path": "/tasks/{{newTaskId}}", "value": {
                  "id": "{{newTaskId}}", "type": "word", "order": 1, "maxScore": 6,
                  "content": {"$type":"delta","ops":[{"insert":"added\n"}]} } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetWorkDataAsync(client, workId);
        data.GetProperty("title").GetString().Should().Be("Combined Update");
        data.GetProperty("tasks").EnumerateArray().Select(t => t.GetProperty("id").GetString())
            .Should().Contain(existingTaskId).And.Contain(newTaskId);
        data.GetProperty("maxScore").GetInt32().Should().Be(2 + 6);
    }

    [Fact(DisplayName = "PATCH /work removing all-but-one task drops MaxScore to the survivor")]
    public async Task Patch_Work_Remove_Task_Recomputes_MaxScore()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId, maxScore: 4);
        var keepId = await GetSingleTaskIdAsync(client, workId);

        var dropId = Ulid.NewUlid().ToString();
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", $$"""
            [ { "op": "add", "path": "/tasks/{{dropId}}", "value": {
                  "id": "{{dropId}}", "type": "word", "order": 1, "maxScore": 10,
                  "content": {"$type":"delta","ops":[{"insert":"x\n"}]} } } ]
            """)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await PatchAsync(client.AsTeacher(), $"/work/{workId}",
            $$"""[ { "op": "remove", "path": "/tasks/{{dropId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetWorkDataAsync(client, workId);
        var tasks = data.GetProperty("tasks").EnumerateArray().ToList();
        tasks.Should().HaveCount(1);
        tasks[0].GetProperty("id").GetString().Should().Be(keepId);
        data.GetProperty("maxScore").GetInt32().Should().Be(4);
    }

    // Regression: the patch pipeline now deep-validates nested DTOs reached through the
    // Tasks dictionary, so [Range(1, int.MaxValue)] on UpdateWorkTaskDTO.MaxScore is
    // enforced on PATCH just like it is for POST /work. A maxScore of 0 must be rejected.
    [Fact(DisplayName = "PATCH /work with an invalid nested task value (maxScore 0) returns 400")]
    public async Task Patch_Work_Invalid_Nested_Task_BadRequest()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId);
        var taskId = await GetSingleTaskIdAsync(client, workId);

        var patch = $$"""
            [ { "op": "replace", "path": "/tasks/{{taskId}}/maxScore", "value": 0 } ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "PATCH /work replacing the whole task value rewrites its fields")]
    public async Task Patch_Work_Replace_Whole_Task_Value()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkWithTaskAsync(client, subjectId);
        var taskId = await GetSingleTaskIdAsync(client, workId);

        var patch = $$"""
            [
              { "op": "replace", "path": "/tasks/{{taskId}}", "value": {
                  "id": "{{taskId}}", "type": "word", "order": 9, "maxScore": 11,
                  "content": {"$type":"delta","ops":[{"insert":"rewritten\n"}]} } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/work/{workId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetWorkDataAsync(client, workId);
        var task = data.GetProperty("tasks").EnumerateArray().Single();
        task.GetProperty("id").GetString().Should().Be(taskId);
        task.GetProperty("order").GetInt32().Should().Be(9);
        task.GetProperty("maxScore").GetInt32().Should().Be(11);
    }
}
