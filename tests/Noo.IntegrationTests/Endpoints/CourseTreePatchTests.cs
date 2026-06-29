using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Response;
using Noo.Api.Courses.Models;
using Noo.Api.Subjects.DTO;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Integration coverage for PATCH /course/{id} — a JSON Patch over
/// <see cref="Noo.Api.Courses.DTO.UpdateCourseDTO"/>. The chapter tree is FLAT for
/// updates: every chapter (root or nested) is a top-level entry in /chapters keyed
/// by Id, and its position is carried by the ParentChapterId scalar. The DTO->Model
/// merge reuses EF-tracked chapters by Id, so adds/edits/removals/moves never orphan
/// existing descendants. Materials remain a one-level dictionary on their chapter.
///
/// Covers top-level field edits, chapter and material scalar edits, removals, adding
/// a parent + nested sub-chapter in one document, moving a material between chapters,
/// and re-parenting a chapter (a one-op parentChapterId change).
/// </summary>
public class CourseTreePatchTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CourseTreePatchTests(ApiFactory factory)
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

    private async Task<Ulid> CreateCourseAsync(HttpClient client, object payload)
    {
        var resp = await client.AsTeacher().PostAsJsonAsync("/course", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        return body!.Data!.Id;
    }

    private async Task<(Ulid courseId, Ulid chapterId)> CreateCourseWithOneChapterAsync(HttpClient client)
    {
        var subjectId = await CreateSubjectAsync(client);
        var courseId = await CreateCourseAsync(client, new
        {
            name = $"Course-{Guid.NewGuid():N}",
            description = "desc",
            subjectId = subjectId.ToString(),
            chapters = new[]
            {
                new
                {
                    title = "Chapter A",
                    order = 0,
                    isActive = true,
                    subChapters = Array.Empty<object>(),
                    materials = Array.Empty<object>(),
                }
            }
        });

        var data = await GetCourseDataAsync(client, courseId);
        var chapters = data.GetProperty("chapters").EnumerateArray().ToList();
        chapters.Should().HaveCount(1);
        return (courseId, Ulid.Parse(chapters[0].GetProperty("id").GetString()!));
    }

    private async Task<JsonElement> GetCourseDataAsync(HttpClient client, Ulid courseId)
    {
        var resp = await client.AsTeacher().GetAsync($"/course/{courseId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
    }

    private static JsonElement Chapter(JsonElement courseData, string chapterId)
        => courseData.GetProperty("chapters").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == chapterId);

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, string body)
        => client.PatchAsync(path, new StringContent(body, Encoding.UTF8, "application/json-patch+json"));

    private static async Task AddMaterialAsync(HttpClient client, Ulid courseId, Ulid chapterId, Ulid materialId, string title, int order)
    {
        var patch = $$"""
            [
              { "op": "add", "path": "/chapters/{{chapterId}}/materials/{{materialId}}", "value": {
                  "id": "{{materialId}}", "title": "{{title}}", "order": {{order}}, "isActive": true } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PATCH /course updates top-level scalar fields")]
    public async Task Patch_Course_TopLevel_Fields()
    {
        using var client = _factory.CreateClient();
        var (courseId, _) = await CreateCourseWithOneChapterAsync(client);

        const string patch = """
            [
              { "op": "replace", "path": "/name", "value": "Renamed Course" },
              { "op": "replace", "path": "/description", "value": "new description" }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetCourseDataAsync(client, courseId);
        data.GetProperty("name").GetString().Should().Be("Renamed Course");
        data.GetProperty("description").GetString().Should().Be("new description");
    }

    [Fact(DisplayName = "PATCH /course updates an existing chapter's scalar fields")]
    public async Task Patch_Course_Update_Chapter_Fields()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId) = await CreateCourseWithOneChapterAsync(client);

        var patch = $$"""
            [
              { "op": "replace", "path": "/chapters/{{chapterId}}/title", "value": "Chapter A (edited)" },
              { "op": "replace", "path": "/chapters/{{chapterId}}/order", "value": 3 },
              { "op": "replace", "path": "/chapters/{{chapterId}}/color", "value": "#FF0000" }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var chapter = Chapter(await GetCourseDataAsync(client, courseId), chapterId.ToString());
        chapter.GetProperty("title").GetString().Should().Be("Chapter A (edited)");
        chapter.GetProperty("order").GetInt32().Should().Be(3);
        chapter.GetProperty("color").GetString().Should().Be("#FF0000");
    }

    [Fact(DisplayName = "PATCH /course updates a material's scalar fields")]
    public async Task Patch_Course_Update_Material_Fields()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId) = await CreateCourseWithOneChapterAsync(client);
        var materialId = Ulid.NewUlid();
        await AddMaterialAsync(client, courseId, chapterId, materialId, "Mat", 0);

        var patch = $$"""
            [
              { "op": "replace", "path": "/chapters/{{chapterId}}/materials/{{materialId}}/title", "value": "Mat (edited)" },
              { "op": "replace", "path": "/chapters/{{chapterId}}/materials/{{materialId}}/order", "value": 5 },
              { "op": "replace", "path": "/chapters/{{chapterId}}/materials/{{materialId}}/isActive", "value": false }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var chapter = Chapter(await GetCourseDataAsync(client, courseId), chapterId.ToString());
        var material = chapter.GetProperty("materials").EnumerateArray()
            .Single(m => m.GetProperty("id").GetString() == materialId.ToString());
        material.GetProperty("title").GetString().Should().Be("Mat (edited)");
        material.GetProperty("order").GetInt32().Should().Be(5);
        material.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact(DisplayName = "PATCH /course removes a material from a chapter")]
    public async Task Patch_Course_Remove_Material()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId) = await CreateCourseWithOneChapterAsync(client);
        var keepId = Ulid.NewUlid();
        var dropId = Ulid.NewUlid();
        await AddMaterialAsync(client, courseId, chapterId, keepId, "Keep", 0);
        await AddMaterialAsync(client, courseId, chapterId, dropId, "Drop", 1);

        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}",
            $$"""[ { "op": "remove", "path": "/chapters/{{chapterId}}/materials/{{dropId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var chapter = Chapter(await GetCourseDataAsync(client, courseId), chapterId.ToString());
        var matIds = chapter.GetProperty("materials").EnumerateArray()
            .Select(m => m.GetProperty("id").GetString()).ToList();
        matIds.Should().ContainSingle().Which.Should().Be(keepId.ToString());
    }

    [Fact(DisplayName = "PATCH /course removes a whole top-level chapter")]
    public async Task Patch_Course_Remove_Chapter()
    {
        using var client = _factory.CreateClient();
        var (courseId, keepChapterId) = await CreateCourseWithOneChapterAsync(client);

        var dropChapterId = Ulid.NewUlid();
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", $$"""
            [ { "op": "add", "path": "/chapters/{{dropChapterId}}", "value": {
                "id": "{{dropChapterId}}", "title": "Doomed", "order": 1, "isActive": true,
                "materials": {} } } ]
            """)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}",
            $$"""[ { "op": "remove", "path": "/chapters/{{dropChapterId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var chapters = (await GetCourseDataAsync(client, courseId)).GetProperty("chapters")
            .EnumerateArray().Select(c => c.GetProperty("id").GetString()).ToList();
        chapters.Should().ContainSingle().Which.Should().Be(keepChapterId.ToString());
    }

    [Fact(DisplayName = "PATCH /course adds a parent chapter, a nested sub-chapter and a material in one document")]
    public async Task Patch_Course_Add_Chapter_With_Nested_Children()
    {
        using var client = _factory.CreateClient();
        var (courseId, _) = await CreateCourseWithOneChapterAsync(client);

        // Flat contract: parent and sub-chapter are both top-level /chapters entries;
        // the sub points at the parent via parentChapterId. The material hangs off the
        // parent's own (one-level) materials dictionary.
        var newChapterId = Ulid.NewUlid();
        var subChapterId = Ulid.NewUlid();
        var materialId = Ulid.NewUlid();
        var patch = $$"""
            [
              { "op": "add", "path": "/chapters/{{newChapterId}}", "value": {
                  "id": "{{newChapterId}}", "title": "Parent", "order": 1, "isActive": true,
                  "materials": {
                    "{{materialId}}": { "id": "{{materialId}}", "title": "M", "order": 0, "isActive": true }
                  } } },
              { "op": "add", "path": "/chapters/{{subChapterId}}", "value": {
                  "id": "{{subChapterId}}", "title": "Sub", "order": 0, "isActive": true,
                  "parentChapterId": "{{newChapterId}}", "materials": {} } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var chapter = Chapter(await GetCourseDataAsync(client, courseId), newChapterId.ToString());
        var subs = chapter.GetProperty("subChapters").EnumerateArray().ToList();
        subs.Should().ContainSingle();
        subs[0].GetProperty("id").GetString().Should().Be(subChapterId.ToString());
        subs[0].GetProperty("parentChapterId").GetString().Should().Be(newChapterId.ToString());

        var mats = chapter.GetProperty("materials").EnumerateArray().ToList();
        mats.Should().ContainSingle();
        mats[0].GetProperty("chapterId").GetString().Should().Be(newChapterId.ToString());
    }

    [Fact(DisplayName = "PATCH /course moves a material from one top-level chapter to another")]
    public async Task Patch_Course_Move_Material_Between_Chapters()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterAId) = await CreateCourseWithOneChapterAsync(client);

        // Add a second top-level chapter B.
        var chapterBId = Ulid.NewUlid();
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", $$"""
            [ { "op": "add", "path": "/chapters/{{chapterBId}}", "value": {
                "id": "{{chapterBId}}", "title": "Chapter B", "order": 1, "isActive": true,
                "materials": {} } } ]
            """)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Seed a material under chapter A.
        var materialId = Ulid.NewUlid();
        await AddMaterialAsync(client, courseId, chapterAId, materialId, "Movable", 0);

        // Move it: remove from A, add under B in the same document.
        var movePatch = $$"""
            [
              { "op": "remove", "path": "/chapters/{{chapterAId}}/materials/{{materialId}}" },
              { "op": "add", "path": "/chapters/{{chapterBId}}/materials/{{materialId}}", "value": {
                  "id": "{{materialId}}", "title": "Movable", "order": 0, "isActive": true } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", movePatch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetCourseDataAsync(client, courseId);
        Chapter(data, chapterAId.ToString()).GetProperty("materials").EnumerateArray().Should().BeEmpty();
        var movedMaterials = Chapter(data, chapterBId.ToString()).GetProperty("materials").EnumerateArray().ToList();
        movedMaterials.Should().ContainSingle();
        movedMaterials[0].GetProperty("id").GetString().Should().Be(materialId.ToString());
        movedMaterials[0].GetProperty("chapterId").GetString().Should().Be(chapterBId.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var material = await db.GetDbSet<CourseMaterialModel>().AsNoTracking()
            .FirstAsync(m => m.Id == materialId);
        material.ChapterId.Should().Be(chapterBId);
    }

    [Fact(DisplayName = "PATCH /course moves a material when emptying the source chapter sends materials:null (frontend pattern)")]
    public async Task Patch_Course_Move_Material_When_Source_Emptied_To_Null()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterAId) = await CreateCourseWithOneChapterAsync(client);

        var chapterBId = Ulid.NewUlid();
        await AddChapterAsync(client, courseId, chapterBId, "Chapter B", 1);

        var materialId = Ulid.NewUlid();
        await AddMaterialAsync(client, courseId, chapterAId, materialId, "Movable", 0);

        // The frontend re-adds the material under B (carrying its now-stale chapterId) and,
        // because A is left with no materials, normalizes A's emptied dictionary to null
        // rather than emitting a per-material remove. The merge must reuse the single tracked
        // material instance (resolved course-wide) instead of minting a duplicate under B.
        var movePatch = $$"""
            [
              { "op": "add", "path": "/chapters/{{chapterBId}}/materials/{{materialId}}", "value": {
                  "id": "{{materialId}}", "title": "Movable", "order": 0, "isActive": true,
                  "chapterId": "{{chapterAId}}" } },
              { "op": "replace", "path": "/chapters/{{chapterAId}}/materials", "value": null }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", movePatch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetCourseDataAsync(client, courseId);
        Chapter(data, chapterAId.ToString()).GetProperty("materials").EnumerateArray().Should().BeEmpty();
        var moved = Chapter(data, chapterBId.ToString()).GetProperty("materials").EnumerateArray().ToList();
        moved.Should().ContainSingle();
        moved[0].GetProperty("id").GetString().Should().Be(materialId.ToString());
        moved[0].GetProperty("chapterId").GetString().Should().Be(chapterBId.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var materials = await db.GetDbSet<CourseMaterialModel>().AsNoTracking()
            .Where(m => m.Id == materialId).ToListAsync();
        materials.Should().ContainSingle(because: "the moved material keeps its single identity");
        materials[0].ChapterId.Should().Be(chapterBId);
    }

    private async Task AddChapterAsync(HttpClient client, Ulid courseId, Ulid chapterId, string title, int order, Ulid? parentChapterId = null)
    {
        var parentLine = parentChapterId is null
            ? string.Empty
            : $"\"parentChapterId\": \"{parentChapterId}\",";
        var patch = $$"""
            [
              { "op": "add", "path": "/chapters/{{chapterId}}", "value": {
                  "id": "{{chapterId}}", "title": "{{title}}", "order": {{order}}, "isActive": true,
                  {{parentLine}} "materials": {} } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "PATCH /course re-parents a sub-chapter from one parent to another in one op")]
    public async Task Patch_Course_Reparent_SubChapter_Between_Parents()
    {
        using var client = _factory.CreateClient();
        var (courseId, parentAId) = await CreateCourseWithOneChapterAsync(client);

        var parentBId = Ulid.NewUlid();
        await AddChapterAsync(client, courseId, parentBId, "Parent B", 1);

        var subId = Ulid.NewUlid();
        await AddChapterAsync(client, courseId, subId, "Sub", 0, parentChapterId: parentAId);

        // Moving the sub-chapter is a single parentChapterId replace.
        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}",
            $$"""[ { "op": "replace", "path": "/chapters/{{subId}}/parentChapterId", "value": "{{parentBId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetCourseDataAsync(client, courseId);
        Chapter(data, parentAId.ToString()).GetProperty("subChapters").EnumerateArray().Should().BeEmpty();
        var bSubs = Chapter(data, parentBId.ToString()).GetProperty("subChapters").EnumerateArray().ToList();
        bSubs.Should().ContainSingle();
        bSubs[0].GetProperty("id").GetString().Should().Be(subId.ToString());
        bSubs[0].GetProperty("parentChapterId").GetString().Should().Be(parentBId.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var moved = await db.GetDbSet<CourseChapterModel>().AsNoTracking().FirstAsync(c => c.Id == subId);
        moved.ParentChapterId.Should().Be(parentBId);
    }

    [Fact(DisplayName = "PATCH /course promotes a sub-chapter to a root by clearing parentChapterId")]
    public async Task Patch_Course_Promote_SubChapter_To_Root()
    {
        using var client = _factory.CreateClient();
        var (courseId, parentId) = await CreateCourseWithOneChapterAsync(client);

        var subId = Ulid.NewUlid();
        await AddChapterAsync(client, courseId, subId, "Sub", 0, parentChapterId: parentId);

        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}",
            $$"""[ { "op": "replace", "path": "/chapters/{{subId}}/parentChapterId", "value": null } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetCourseDataAsync(client, courseId);
        Chapter(data, parentId.ToString()).GetProperty("subChapters").EnumerateArray().Should().BeEmpty();
        var rootIds = data.GetProperty("chapters").EnumerateArray()
            .Select(c => c.GetProperty("id").GetString()).ToList();
        rootIds.Should().Contain(subId.ToString(), because: "clearing parentChapterId makes the chapter a root");
    }

    [Fact(DisplayName = "PATCH /course re-parenting a sub-chapter keeps its own descendants")]
    public async Task Patch_Course_Reparent_SubChapter_Keeps_Descendants()
    {
        using var client = _factory.CreateClient();
        var (courseId, parentAId) = await CreateCourseWithOneChapterAsync(client);

        var parentBId = Ulid.NewUlid();
        await AddChapterAsync(client, courseId, parentBId, "Parent B", 1);

        // A -> Sub -> Leaf, then move Sub under B; Leaf must stay under Sub.
        var subId = Ulid.NewUlid();
        var leafId = Ulid.NewUlid();
        await AddChapterAsync(client, courseId, subId, "Sub", 0, parentChapterId: parentAId);
        await AddChapterAsync(client, courseId, leafId, "Leaf", 0, parentChapterId: subId);

        (await PatchAsync(client.AsTeacher(), $"/course/{courseId}",
            $$"""[ { "op": "replace", "path": "/chapters/{{subId}}/parentChapterId", "value": "{{parentBId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var data = await GetCourseDataAsync(client, courseId);
        var movedSub = Chapter(data, parentBId.ToString()).GetProperty("subChapters").EnumerateArray().Single();
        movedSub.GetProperty("id").GetString().Should().Be(subId.ToString());
        var leaves = movedSub.GetProperty("subChapters").EnumerateArray().ToList();
        leaves.Should().ContainSingle();
        leaves[0].GetProperty("id").GetString().Should().Be(leafId.ToString());
    }

    [Fact(DisplayName = "PATCH /course as student returns 403 Forbidden")]
    public async Task Patch_Course_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var (courseId, _) = await CreateCourseWithOneChapterAsync(client);
        (await PatchAsync(client.AsStudent(), $"/course/{courseId}",
            """[ { "op": "replace", "path": "/name", "value": "X" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PATCH /course for a non-existing id returns 404 Not Found")]
    public async Task Patch_Course_NotFound()
    {
        using var client = _factory.CreateClient();
        (await PatchAsync(client.AsTeacher(), $"/course/{Ulid.NewUlid()}",
            """[ { "op": "replace", "path": "/name", "value": "X" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
