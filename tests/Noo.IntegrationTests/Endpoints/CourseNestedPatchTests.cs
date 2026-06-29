using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Types;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Regression coverage for the nested-dictionary-merge code path that backs every
/// PATCH-with-children endpoint that uses
/// <see cref="Noo.Api.Core.Utils.AutoMapper.NestedEntityMappingExtensions.MapDictionaryToCollection"/>:
///
///   - PATCH /course           — Chapters dictionary
///   - PATCH /course           — SubChapters dictionary (nested under a chapter)
///   - PATCH /course           — Materials dictionary (nested under a chapter)
///   - PATCH /course/material-content — WorkAssignments dictionary
///
/// Each test seeds a parent with one existing child, then issues a JSON Patch
/// that ADDs a new child and verifies the existing child's Id is preserved
/// (and not silently re-keyed to a fresh Ulid, which would orphan its DB row
/// and cascade-corrupt linked tables — the original assigned_work_answer bug).
/// </summary>
public class CourseNestedPatchTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CourseNestedPatchTests(ApiFactory factory)
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

    private static async Task<Ulid> CreateWorkAsync(HttpClient client, Ulid subjectId)
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
                    maxScore = 1,
                    content = RichTextFactory.Create("Q"),
                }
            }
        };
        var resp = await client.AsTeacher().PostAsJsonAsync("/work", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        return body!.Data!.Id;
    }

    private async Task<(Ulid courseId, Ulid existingChapterId)> CreateCourseWithOneChapterAsync(HttpClient client)
    {
        var subjectId = await CreateSubjectAsync(client);

        var payload = new
        {
            name = $"Course-{Guid.NewGuid():N}",
            description = "desc",
            subjectId = subjectId.ToString(),
            chapters = new[]
            {
                new
                {
                    title = "Existing Chapter",
                    order = 0,
                    isActive = true,
                    subChapters = Array.Empty<object>(),
                    materials = Array.Empty<object>(),
                }
            }
        };

        var resp = await client.AsTeacher().PostAsJsonAsync("/course", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        var courseId = body!.Data!.Id;

        var getResp = await client.AsTeacher().GetAsync($"/course/{courseId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var chapters = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("chapters").EnumerateArray().ToList();
        chapters.Should().HaveCount(1);
        return (courseId, Ulid.Parse(chapters[0].GetProperty("id").GetString()!));
    }

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, string body)
        => client.PatchAsync(path, new StringContent(body, Encoding.UTF8, "application/json-patch+json"));

    [Fact(DisplayName = "PATCH /course adds a chapter; existing chapter keeps its Id")]
    public async Task Patch_Course_Add_Chapter_Preserves_Existing()
    {
        using var client = _factory.CreateClient();
        var (courseId, existingChapterId) = await CreateCourseWithOneChapterAsync(client);

        var newChapterId = Ulid.NewUlid();
        var patchJson = $$"""
            [
              {
                "op": "add",
                "path": "/chapters/{{newChapterId}}",
                "value": {
                  "id": "{{newChapterId}}",
                  "title": "New Chapter",
                  "order": 1,
                  "isActive": true,
                  "materials": {}
                }
              }
            ]
            """;

        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patchJson))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResp = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var chaptersAfter = JsonDocument.Parse(await afterResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("chapters").EnumerateArray().ToList();

        chaptersAfter.Should().HaveCount(2);
        var ids = chaptersAfter.ConvertAll(c => c.GetProperty("id").GetString());
        ids.Should().Contain(existingChapterId.ToString(), because: "the existing chapter's Id must survive the patch");
        ids.Should().Contain(newChapterId.ToString());
    }

    // KNOWN FAILURE — separate from the mapper merge bug this suite primarily guards.
    // Regression for the original data-loss bug: adding a sub-chapter to a chapter that
    // already has one used to silently wipe the existing sibling. With the flattened
    // update contract every chapter (root or nested) is a top-level entry in
    // /chapters keyed by Id, and parentage is a plain ParentChapterId field — so the
    // Id-keyed merge sees the existing sub-chapter in the document and never orphans it.
    [Fact(DisplayName = "PATCH /course adds a sub-chapter; existing parent + sub-chapter Ids preserved")]
    public async Task Patch_Course_Add_SubChapter_Preserves_Existing()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);

        // Build a course that already has one chapter with one sub-chapter, so
        // the merge path runs against a pre-existing nested child loaded from DB.
        var coursePayload = new
        {
            name = $"Course-{Guid.NewGuid():N}",
            subjectId = subjectId.ToString(),
            chapters = new[]
            {
                new
                {
                    title = "Parent",
                    order = 0,
                    isActive = true,
                    subChapters = new[]
                    {
                        new
                        {
                            title = "Existing Sub",
                            order = 0,
                            isActive = true,
                            subChapters = Array.Empty<object>(),
                            materials = Array.Empty<object>(),
                        }
                    },
                    materials = Array.Empty<object>(),
                }
            }
        };
        var createResp = await client.AsTeacher().PostAsJsonAsync("/course", coursePayload, JsonOptions);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var courseId = (await createResp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions))!.Data!.Id;

        var beforeResp = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var before = JsonDocument.Parse(await beforeResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("chapters").EnumerateArray().ToList();
        before.Should().HaveCount(1);
        var parentChapterId = before[0].GetProperty("id").GetString()!;
        var existingSubs = before[0].GetProperty("subChapters").EnumerateArray().ToList();
        existingSubs.Should().HaveCount(1);
        var existingSubChapterId = existingSubs[0].GetProperty("id").GetString()!;

        // Flat contract: the new sub-chapter is a top-level /chapters entry whose
        // parentChapterId points at the existing parent.
        var newSubChapterId = Ulid.NewUlid();
        var addPatch = $$"""
            [
              {
                "op": "add",
                "path": "/chapters/{{newSubChapterId}}",
                "value": {
                  "id": "{{newSubChapterId}}",
                  "title": "New Sub",
                  "order": 1,
                  "isActive": true,
                  "parentChapterId": "{{parentChapterId}}",
                  "materials": {}
                }
              }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", addPatch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResp = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var chapter = JsonDocument.Parse(await afterResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("chapters").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == parentChapterId);

        var subs = chapter.GetProperty("subChapters").EnumerateArray().ToList();
        subs.Should().HaveCount(2);
        var subIds = subs.ConvertAll(s => s.GetProperty("id").GetString());
        subIds.Should().Contain(existingSubChapterId,
            because: "the seeded sub-chapter's Id must survive the patch — otherwise EF orphans linked rows");
        subIds.Should().Contain(newSubChapterId.ToString());
    }

    [Fact(DisplayName = "PATCH /course adds a material; existing material Id preserved")]
    public async Task Patch_Course_Add_Material_Preserves_Existing()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId) = await CreateCourseWithOneChapterAsync(client);

        // Seed an existing material.
        var existingMaterialId = Ulid.NewUlid();
        var seedPatch = $$"""
            [
              {
                "op": "add",
                "path": "/chapters/{{chapterId}}/materials/{{existingMaterialId}}",
                "value": {
                  "id": "{{existingMaterialId}}",
                  "title": "Existing Material",
                  "order": 0,
                  "isActive": true
                }
              }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", seedPatch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var newMaterialId = Ulid.NewUlid();
        var addPatch = $$"""
            [
              {
                "op": "add",
                "path": "/chapters/{{chapterId}}/materials/{{newMaterialId}}",
                "value": {
                  "id": "{{newMaterialId}}",
                  "title": "New Material",
                  "order": 1,
                  "isActive": true
                }
              }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", addPatch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterResp = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var chapter = JsonDocument.Parse(await afterResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("chapters").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == chapterId.ToString());

        var materials = chapter.GetProperty("materials").EnumerateArray().ToList();
        materials.Should().HaveCount(2);
        var matIds = materials.ConvertAll(m => m.GetProperty("id").GetString());
        matIds.Should().Contain(existingMaterialId.ToString());
        matIds.Should().Contain(newMaterialId.ToString());
    }

    [Fact(DisplayName = "PATCH /course/material-content adds a work-assignment; existing one preserved")]
    public async Task Patch_MaterialContent_Add_WorkAssignment_Preserves_Existing()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var firstWorkId = await CreateWorkAsync(client, subjectId);
        var secondWorkId = await CreateWorkAsync(client, subjectId);

        // Create a material-content with one existing work-assignment.
        var createContentPayload = new
        {
            content = RichTextFactory.Create("body"),
            nootubeVideoIds = Array.Empty<string>(),
            mediaIds = Array.Empty<string>(),
            workAssignments = new[]
            {
                new
                {
                    order = 0,
                    workId = firstWorkId.ToString(),
                    note = "Existing",
                    isActive = true,
                }
            }
        };
        var createResp = await client.AsTeacher()
            .PostAsJsonAsync("/course/material-content", createContentPayload, JsonOptions);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var contentId = (await createResp.Content
            .ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions))!.Data!.Id;

        // The created content's existing work-assignment Id isn't returned by POST,
        // but we don't need it: we'll assert after the patch that exactly two
        // work-assignments exist with the right workIds, and the "Existing" note
        // survived (proving the existing row wasn't rewritten).
        var newAssignmentId = Ulid.NewUlid();
        var patchJson = $$"""
            [
              {
                "op": "add",
                "path": "/workAssignments/{{newAssignmentId}}",
                "value": {
                  "order": 1,
                  "workId": "{{secondWorkId}}",
                  "note": "New",
                  "isActive": true
                }
              }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}", patchJson))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // No direct GET for material-content alone in WorkAssignments, but we can use
        // /course/{courseId}/content/{contentId} — which currently isn't wired for a
        // standalone content. Instead, assert via the work-assignment list on the
        // patched record by reading through the database via the test factory.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Noo.Api.Core.DataAbstraction.Db.NooDbContext>();
        var content = await db
            .GetDbSet<Noo.Api.Courses.Models.CourseMaterialContentModel>()
            .Include(c => c.WorkAssignments)
            .FirstAsync(c => c.Id == contentId);

        content.WorkAssignments!.Should().HaveCount(2);
        var notes = content.WorkAssignments!.Select(a => a.Note).ToList();
        notes.Should().Contain("Existing", because: "the seeded work-assignment row must survive the patch");
        notes.Should().Contain("New");
        content.WorkAssignments!.Should().Contain(a => a.Id == newAssignmentId);
    }

    [Fact(DisplayName = "PATCH /course/material-content replaces rich text content and persists it")]
    public async Task Patch_MaterialContent_Replaces_RichText_Content()
    {
        using var client = _factory.CreateClient();

        // Create a material-content whose rich text has a single insert op.
        var createContentPayload = new
        {
            content = RichTextFactory.Create("body"),
            nootubeVideoIds = Array.Empty<string>(),
            mediaIds = Array.Empty<string>(),
            workAssignments = Array.Empty<object>(),
        };
        var createResp = await client.AsTeacher()
            .PostAsJsonAsync("/course/material-content", createContentPayload, JsonOptions);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var contentId = (await createResp.Content
            .ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions))!.Data!.Id;

        // Replace the text node in place. This mutates the existing rich text instance,
        // which EF Core only detects with a structural value comparer on the converter.
        const string patchJson = """
            [
              { "op": "replace", "path": "/content/content/0/content/0/text", "value": "patched" }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}", patchJson))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Noo.Api.Core.DataAbstraction.Db.NooDbContext>();
        var content = await db
            .GetDbSet<Noo.Api.Courses.Models.CourseMaterialContentModel>()
            .AsNoTracking()
            .FirstAsync(c => c.Id == contentId);

        content.Content!.ToString().Should().Be("patched");
    }
}
