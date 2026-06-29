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
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;
using Noo.Api.Polls.Models;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Types;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Integration coverage for PATCH /course/material-content/{id} — a JSON Patch
/// over <see cref="Noo.Api.Courses.DTO.UpdateCourseMaterialContentDTO"/>.
///
/// The content carries a nested WorkAssignments dictionary (keyed by Id, merged
/// via the same NestedEntityMappingExtensions path as course chapters/work
/// tasks), a Poll FK, and Medias / NooTubeVideos id-reference dictionaries
/// resolved in CourseService through IEntityReferenceFactory. These cover
/// adding/updating/removing work-assignments, attaching and detaching a poll,
/// and setting the media/video reference dictionaries.
/// </summary>
public class MaterialContentPatchTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MaterialContentPatchTests(ApiFactory factory)
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

    private async Task<Ulid> SeedPollAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var poll = new PollModel
        {
            Title = $"Poll-{Guid.NewGuid():N}",
            IsActive = true,
            IsAuthRequired = false,
        };
        db.GetDbSet<PollModel>().Add(poll);
        await db.SaveChangesAsync();
        return poll.Id;
    }

    private async Task<Ulid> SeedMediaAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var media = new MediaModel
        {
            Path = $"uploads/{Guid.NewGuid():N}.png",
            Name = "image.png",
            Extension = "png",
            Category = MediaCategory.CourseAttachment,
            Status = MediaStatus.Completed,
            OwnerId = Ulid.NewUlid(),
        };
        db.GetDbSet<MediaModel>().Add(media);
        await db.SaveChangesAsync();
        return media.Id;
    }

    private async Task<Ulid> SeedVideoAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var video = new NooTubeVideoModel
        {
            Title = $"Video-{Guid.NewGuid():N}",
            ServiceType = NooTubeServiceType.Kinescope,
            State = VideoState.Published,
            IsActive = true,
            IsListed = true,
        };
        db.GetDbSet<NooTubeVideoModel>().Add(video);
        await db.SaveChangesAsync();
        return video.Id;
    }

    private async Task<Ulid> CreateContentAsync(HttpClient client, object? workAssignments = null)
    {
        var payload = new
        {
            content = RichTextFactory.Create("body"),
            nootubeVideoIds = Array.Empty<string>(),
            mediaIds = Array.Empty<string>(),
            workAssignments = workAssignments ?? Array.Empty<object>(),
        };
        var resp = await client.AsTeacher().PostAsJsonAsync("/course/material-content", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        return body!.Data!.Id;
    }

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, string body)
        => client.PatchAsync(path, new StringContent(body, Encoding.UTF8, "application/json-patch+json"));

    private async Task<CourseMaterialContentModel> LoadContentAsync(Ulid contentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        return await db.GetDbSet<CourseMaterialContentModel>()
            .Include(c => c.WorkAssignments)
            .Include(c => c.Medias)
            .Include(c => c.NooTubeVideos)
            .AsNoTracking()
            .FirstAsync(c => c.Id == contentId);
    }

    [Fact(DisplayName = "PATCH /material-content updates an existing work-assignment's fields")]
    public async Task Patch_Content_Update_WorkAssignment_Fields()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId);

        var assignmentId = Ulid.NewUlid();
        var contentId = await CreateContentAsync(client, new[]
        {
            new { id = assignmentId.ToString(), order = 0, workId = workId.ToString(), note = "Before", isActive = true }
        });

        // Work-assignment Ids are server-assigned on create, so discover the real one.
        var seeded = await LoadContentAsync(contentId);
        var realId = seeded.WorkAssignments!.Single().Id;

        var patch = $$"""
            [
              { "op": "replace", "path": "/workAssignments/{{realId}}/note", "value": "After" },
              { "op": "replace", "path": "/workAssignments/{{realId}}/order", "value": 2 },
              { "op": "replace", "path": "/workAssignments/{{realId}}/isActive", "value": false }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var content = await LoadContentAsync(contentId);
        var assignment = content.WorkAssignments!.Single();
        assignment.Id.Should().Be(realId);
        assignment.Note.Should().Be("After");
        assignment.Order.Should().Be(2);
        assignment.IsActive.Should().BeFalse();
    }

    [Fact(DisplayName = "PATCH /material-content removes a work-assignment; sibling preserved")]
    public async Task Patch_Content_Remove_WorkAssignment()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var firstWorkId = await CreateWorkAsync(client, subjectId);
        var secondWorkId = await CreateWorkAsync(client, subjectId);

        var contentId = await CreateContentAsync(client, new[]
        {
            new { order = 0, workId = firstWorkId.ToString(), note = "Keep", isActive = true },
            new { order = 1, workId = secondWorkId.ToString(), note = "Drop", isActive = true },
        });

        var seeded = await LoadContentAsync(contentId);
        var dropId = seeded.WorkAssignments!.Single(a => a.Note == "Drop").Id;

        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}",
            $$"""[ { "op": "remove", "path": "/workAssignments/{{dropId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var content = await LoadContentAsync(contentId);
        content.WorkAssignments!.Should().ContainSingle().Which.Note.Should().Be("Keep");
    }

    [Fact(DisplayName = "PATCH /material-content attaches a poll by id")]
    public async Task Patch_Content_Attach_Poll()
    {
        using var client = _factory.CreateClient();
        var pollId = await SeedPollAsync();
        var contentId = await CreateContentAsync(client);

        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}",
            $$"""[ { "op": "replace", "path": "/pollId", "value": "{{pollId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await LoadContentAsync(contentId)).PollId.Should().Be(pollId);
    }

    [Fact(DisplayName = "PATCH /material-content detaches a previously attached poll")]
    public async Task Patch_Content_Detach_Poll()
    {
        using var client = _factory.CreateClient();
        var pollId = await SeedPollAsync();
        var contentId = await CreateContentAsync(client);

        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}",
            $$"""[ { "op": "replace", "path": "/pollId", "value": "{{pollId}}" } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LoadContentAsync(contentId)).PollId.Should().Be(pollId);

        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}",
            """[ { "op": "replace", "path": "/pollId", "value": null } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LoadContentAsync(contentId)).PollId.Should().BeNull();
    }

    [Fact(DisplayName = "PATCH /material-content sets the medias id-reference dictionary")]
    public async Task Patch_Content_Set_Medias()
    {
        using var client = _factory.CreateClient();
        var contentId = await CreateContentAsync(client);
        var mediaId = await SeedMediaAsync();

        var patch = $$"""
            [ { "op": "add", "path": "/medias/{{mediaId}}", "value": { "id": "{{mediaId}}" } } ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var content = await LoadContentAsync(contentId);
        content.Medias!.Select(m => m.Id).Should().ContainSingle().Which.Should().Be(mediaId);
    }

    [Fact(DisplayName = "PATCH /material-content sets the nootube-videos id-reference dictionary")]
    public async Task Patch_Content_Set_NooTubeVideos()
    {
        using var client = _factory.CreateClient();
        var contentId = await CreateContentAsync(client);
        var videoId = await SeedVideoAsync();

        var patch = $$"""
            [ { "op": "add", "path": "/nootubeVideos/{{videoId}}", "value": { "id": "{{videoId}}" } } ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var content = await LoadContentAsync(contentId);
        content.NooTubeVideos!.Select(v => v.Id).Should().ContainSingle().Which.Should().Be(videoId);
    }

    [Fact(DisplayName = "PATCH /material-content combines a work-assignment add, poll attach and media set")]
    public async Task Patch_Content_Combined_Operations()
    {
        using var client = _factory.CreateClient();
        var subjectId = await CreateSubjectAsync(client);
        var workId = await CreateWorkAsync(client, subjectId);
        var pollId = await SeedPollAsync();
        var contentId = await CreateContentAsync(client);
        var mediaId = await SeedMediaAsync();

        var assignmentId = Ulid.NewUlid();
        var patch = $$"""
            [
              { "op": "replace", "path": "/pollId", "value": "{{pollId}}" },
              { "op": "add", "path": "/medias/{{mediaId}}", "value": { "id": "{{mediaId}}" } },
              { "op": "add", "path": "/workAssignments/{{assignmentId}}", "value": {
                  "order": 0, "workId": "{{workId}}", "note": "Combined", "isActive": true } }
            ]
            """;
        (await PatchAsync(client.AsTeacher(), $"/course/material-content/{contentId}", patch))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var content = await LoadContentAsync(contentId);
        content.PollId.Should().Be(pollId);
        content.Medias!.Select(m => m.Id).Should().Contain(mediaId);
        content.WorkAssignments!.Should().ContainSingle().Which.Note.Should().Be("Combined");
    }

    [Fact(DisplayName = "PATCH /material-content as student returns 403 Forbidden")]
    public async Task Patch_Content_AsStudent_Forbidden()
    {
        using var client = _factory.CreateClient();
        var contentId = await CreateContentAsync(client);
        (await PatchAsync(client.AsStudent(), $"/course/material-content/{contentId}",
            """[ { "op": "replace", "path": "/pollId", "value": null } ]"""))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
