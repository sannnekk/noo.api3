using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Response;
using Noo.Api.Polls.Models;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Integration coverage for PATCH /poll/{id}, whose body is a JSON Patch over
/// <see cref="Noo.Api.Polls.DTO.UpdatePollDTO"/>. The Questions child collection is
/// exposed to the patch as a dictionary keyed by question Id and merged back into the
/// EF-tracked entity via
/// <see cref="Noo.Api.Core.Utils.AutoMapper.NestedEntityMappingExtensions.MapDictionaryToCollection"/>.
///
/// Regression origin: UpdatePollDTO had no Questions member at all, so every
/// /questions/... operation resolved to a non-existent path. Those errors are swallowed
/// by ApplyToAndValidate, so the endpoint answered 204 while dropping the question edits
/// on the floor. On top of that the write path loaded the poll without its questions, so
/// once the dictionary existed an unrelated title patch would have merged against an
/// empty collection and cascade-deleted every question.
/// </summary>
public class PollNestedPatchTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PollNestedPatchTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Ulid> CreatePollAsync(HttpClient client, params string[] questionTitles)
    {
        var payload = new
        {
            title = $"Poll-{Guid.NewGuid():N}",
            description = "d",
            isActive = true,
            isAuthRequired = false,
            questions = questionTitles
                .Select((title, index) => new
                {
                    title,
                    description = (string?)null,
                    isRequired = false,
                    order = index + 1,
                    type = "text",
                    config = new { type = "text", maxTextLength = 100 },
                })
                .ToArray(),
        };

        var response = await client.AsTeacher().PostAsJsonAsync("/poll", payload, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);

        return body!.Data!.Id;
    }

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, string body)
        => client.PatchAsync(path, new StringContent(body, Encoding.UTF8, "application/json-patch+json"));

    private async Task<JsonElement> GetPollDataAsync(HttpClient client, Ulid pollId)
    {
        var response = await client.AsTeacher().GetAsync($"/poll/{pollId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
    }

    private async Task<List<JsonElement>> GetQuestionsAsync(HttpClient client, Ulid pollId)
    {
        var data = await GetPollDataAsync(client, pollId);

        return data.GetProperty("questions").EnumerateArray().ToList();
    }

    [Fact(DisplayName = "PATCH /poll updates an existing question and adds a new one keyed by a placeholder")]
    public async Task Patch_Poll_Update_And_Add_Questions()
    {
        using var client = _factory.CreateClient();
        var pollId = await CreatePollAsync(client, "Q1");
        var existingId = (await GetQuestionsAsync(client, pollId))[0].GetProperty("id").GetString()!;

        // Shaped exactly like the frontend patch: existing questions are keyed by Id,
        // added ones by a client-side placeholder key.
        var patch = $$"""
            [
              { "op": "replace", "path": "/questions/{{existingId}}/type", "value": "multiple-choice" },
              { "op": "replace", "path": "/questions/{{existingId}}/title", "value": "Q1 patched" },
              { "op": "add", "path": "/questions/new-5", "value": {
                  "_entityName": "PollQuestion", "_key": "4", "order": 2,
                  "title": "Added question", "description": "eeee",
                  "type": "files", "isRequired": true, "config": { "type": "files" } } }
            ]
            """;

        (await PatchAsync(client.AsTeacher(), $"/poll/{pollId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var questions = await GetQuestionsAsync(client, pollId);
        questions.Should().HaveCount(2);

        var updated = questions.Single(question => question.GetProperty("id").GetString() == existingId);
        updated.GetProperty("title").GetString().Should().Be("Q1 patched");
        updated.GetProperty("type").GetString().Should().Be("multiple-choice");

        var added = questions.Single(question => question.GetProperty("id").GetString() != existingId);
        added.GetProperty("title").GetString().Should().Be("Added question");
        added.GetProperty("description").GetString().Should().Be("eeee");
        added.GetProperty("type").GetString().Should().Be("files");
        added.GetProperty("isRequired").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "PATCH /poll touching only the title leaves the questions untouched")]
    public async Task Patch_Poll_TopLevel_Only_Keeps_Questions()
    {
        using var client = _factory.CreateClient();
        var pollId = await CreatePollAsync(client, "Q1", "Q2");
        var existingIds = (await GetQuestionsAsync(client, pollId))
            .ConvertAll(question => question.GetProperty("id").GetString());

        (await PatchAsync(client.AsTeacher(), $"/poll/{pollId}",
            """[ { "op": "replace", "path": "/title", "value": "Only the title" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetPollDataAsync(client, pollId);
        data.GetProperty("title").GetString().Should().Be("Only the title");
        data.GetProperty("questions").EnumerateArray()
            .Select(question => question.GetProperty("id").GetString())
            .Should().BeEquivalentTo(existingIds);

        // The rows themselves must survive, not just the response projection.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var persisted = await db.GetDbSet<PollQuestionModel>().AsNoTracking()
            .CountAsync(question => question.PollId == pollId);
        persisted.Should().Be(2);
    }

    [Fact(DisplayName = "PATCH /poll removing a question drops only that question")]
    public async Task Patch_Poll_Remove_Question()
    {
        using var client = _factory.CreateClient();
        var pollId = await CreatePollAsync(client, "keep", "drop");
        var questions = await GetQuestionsAsync(client, pollId);
        var keepId = questions.Single(q => q.GetProperty("title").GetString() == "keep")
            .GetProperty("id").GetString()!;
        var dropId = questions.Single(q => q.GetProperty("title").GetString() == "drop")
            .GetProperty("id").GetString()!;

        (await PatchAsync(client.AsTeacher(), $"/poll/{pollId}",
            $$"""[ { "op": "remove", "path": "/questions/{{dropId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await GetQuestionsAsync(client, pollId);
        remaining.Should().HaveCount(1);
        remaining[0].GetProperty("id").GetString().Should().Be(keepId);
    }

    [Fact(DisplayName = "PATCH /poll reordering questions is reflected in the read order")]
    public async Task Patch_Poll_Reorder_Questions()
    {
        using var client = _factory.CreateClient();
        var pollId = await CreatePollAsync(client, "first", "second");
        var questions = await GetQuestionsAsync(client, pollId);
        var firstId = questions[0].GetProperty("id").GetString()!;
        var secondId = questions[1].GetProperty("id").GetString()!;

        var patch = $$"""
            [
              { "op": "replace", "path": "/questions/{{firstId}}/order", "value": 2 },
              { "op": "replace", "path": "/questions/{{secondId}}/order", "value": 1 }
            ]
            """;

        (await PatchAsync(client.AsTeacher(), $"/poll/{pollId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reordered = await GetQuestionsAsync(client, pollId);
        reordered.Select(question => question.GetProperty("id").GetString())
            .Should().Equal(secondId, firstId);
    }

    [Fact(DisplayName = "PATCH /poll combines a top-level change and a nested question change")]
    public async Task Patch_Poll_TopLevel_And_Nested_In_One_Document()
    {
        using var client = _factory.CreateClient();
        var pollId = await CreatePollAsync(client, "Q1");
        var questionId = (await GetQuestionsAsync(client, pollId))[0].GetProperty("id").GetString()!;

        var patch = $$"""
            [
              { "op": "replace", "path": "/isActive", "value": false },
              { "op": "replace", "path": "/questions/{{questionId}}/isRequired", "value": true },
              { "op": "replace", "path": "/questions/{{questionId}}/config", "value": {
                  "type": "text", "minTextLength": 5, "maxTextLength": 50 } }
            ]
            """;

        (await PatchAsync(client.AsTeacher(), $"/poll/{pollId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetPollDataAsync(client, pollId);
        data.GetProperty("isActive").GetBoolean().Should().BeFalse();

        var question = data.GetProperty("questions").EnumerateArray().Single();
        question.GetProperty("isRequired").GetBoolean().Should().BeTrue();
        question.GetProperty("config").GetProperty("minTextLength").GetInt32().Should().Be(5);
        question.GetProperty("config").GetProperty("maxTextLength").GetInt32().Should().Be(50);
    }

    // The patch pipeline deep-validates nested DTOs reached through the Questions
    // dictionary, so [MaxLength(255)] on UpdatePollQuestionDTO.Title is enforced on
    // PATCH just like it is for POST /poll.
    [Fact(DisplayName = "PATCH /poll with an over-long nested question title returns 400")]
    public async Task Patch_Poll_Invalid_Nested_Question_BadRequest()
    {
        using var client = _factory.CreateClient();
        var pollId = await CreatePollAsync(client, "Q1");
        var questionId = (await GetQuestionsAsync(client, pollId))[0].GetProperty("id").GetString()!;

        var patch = $$"""
            [ { "op": "replace", "path": "/questions/{{questionId}}/title", "value": "{{new string('x', 300)}}" } ]
            """;

        (await PatchAsync(client.AsTeacher(), $"/poll/{pollId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
