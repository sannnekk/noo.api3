using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;
using Noo.Api.MediaDownloads.Models;
using Noo.Api.Subjects.Models;

namespace Noo.IntegrationTests.Endpoints;

public class MaterialFileDownloadTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MaterialFileDownloadTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static List<JsonElement> ItemsOf(string body)
    {
        return JsonDocument
            .Parse(body)
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .ToList();
    }

    private record Fixture(Ulid CourseId, Ulid MaterialId, Ulid FirstMediaId, Ulid SecondMediaId);

    /// <summary>
    /// A course, material and two attachments of its own, so rows seeded here cannot collide with
    /// those of another test sharing the fixture.
    /// </summary>
    private async Task<Fixture> SeedMaterialAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");

        var subject = new SubjectModel { Name = $"subject-{Ulid.NewUlid()}", Color = "red" };
        db.GetDbSet<SubjectModel>().Add(subject);

        var course = new CourseModel { Name = $"course-{Ulid.NewUlid()}", SubjectId = subject.Id };
        db.GetDbSet<CourseModel>().Add(course);

        var chapter = new CourseChapterModel
        {
            Title = "Chapter",
            CourseId = course.Id,
            IsActive = true,
        };
        db.GetDbSet<CourseChapterModel>().Add(chapter);

        var content = new CourseMaterialContentModel();
        db.GetDbSet<CourseMaterialContentModel>().Add(content);

        var first = MakeAttachment(teacherId, content.Id, "first.pdf");
        var second = MakeAttachment(teacherId, content.Id, "second.pdf");
        db.GetDbSet<MediaModel>().AddRange(first, second);

        content.Medias = [first, second];

        var material = new CourseMaterialModel
        {
            Title = "Material",
            ChapterId = chapter.Id,
            ContentId = content.Id,
            IsActive = true,
        };
        db.GetDbSet<CourseMaterialModel>().Add(material);

        await db.SaveChangesAsync();

        return new Fixture(course.Id, material.Id, first.Id, second.Id);
    }

    private static MediaModel MakeAttachment(Ulid ownerId, Ulid contentId, string name) =>
        new()
        {
            Path = $"course-attachment/{ownerId}/{name}",
            Name = name,
            ActualName = name,
            Extension = "pdf",
            Size = 2048,
            Category = MediaCategory.CourseAttachment,
            Status = MediaStatus.Completed,
            // The uploader tags an attachment with the content id, not the course id.
            EntityId = contentId,
            OwnerId = ownerId,
        };

    private async Task EnrollAsync(Ulid courseId, Ulid studentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        db.GetDbSet<CourseMembershipModel>()
            .Add(
                new CourseMembershipModel
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    IsActive = true,
                }
            );

        await db.SaveChangesAsync();
    }

    private async Task RecordDownloadAsync(Ulid mediaId, Ulid userId, Ulid materialId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        db.GetDbSet<MediaDownloadModel>()
            .Add(
                new MediaDownloadModel
                {
                    MediaId = mediaId,
                    UserId = userId,
                    CourseMaterialId = materialId,
                }
            );

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Domain events are handled off a background dispatcher, so the row lands shortly after the
    /// response rather than with it.
    /// </summary>
    private async Task<int> WaitForDownloadCountAsync(Ulid mediaId, int expected)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

            var count = await db.GetDbSet<MediaDownloadModel>()
                .CountAsync(d => d.MediaId == mediaId);

            if (count >= expected)
            {
                return count;
            }

            await Task.Delay(100);
        }

        return 0;
    }

    [Fact(DisplayName = "GET /media/{id}/download-url as an enrolled student returns 200")]
    public async Task Download_AsEnrolledStudent_IsAllowed()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        await EnrollAsync(fixture.CourseId, studentId);

        // AsUserId rather than AsStudent: the latter mints a random user id, which no membership
        // could ever point at.
        var resp = await client
            .AsUserId(studentId)
            .GetAsync($"/media/{fixture.FirstMediaId}/download-url");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /media/{id}/download-url as an unenrolled student returns 403")]
    public async Task Download_AsUnenrolledStudent_IsForbidden()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();

        var name = $"outsider-{Ulid.NewUlid()}";
        var outsiderId = await TestDataHelpers.CreateUserAsync(
            _factory,
            name,
            $"{name}@example.com",
            "Password1!"
        );

        var resp = await client
            .AsUserId(outsiderId)
            .GetAsync($"/media/{fixture.FirstMediaId}/download-url");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Downloading an attachment records a row against its material")]
    public async Task Download_RecordsAStatisticsRow()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        await EnrollAsync(fixture.CourseId, studentId);

        var resp = await client
            .AsUserId(studentId)
            .GetAsync($"/media/{fixture.FirstMediaId}/download-url");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var count = await WaitForDownloadCountAsync(fixture.FirstMediaId, 1);
        count.Should().BeGreaterThanOrEqualTo(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
        var row = await db.GetDbSet<MediaDownloadModel>()
            .FirstAsync(d => d.MediaId == fixture.FirstMediaId);

        row.UserId.Should().Be(studentId);
        row.CourseMaterialId.Should().Be(fixture.MaterialId);
    }

    [Fact(DisplayName = "GET material file-downloads summary counts downloads and unique users")]
    public async Task Summary_AsTeacher_CountsDownloads()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();

        var studentId = TestDataHelpers.GetUserId(_factory, "student");
        var mentorId = TestDataHelpers.GetUserId(_factory, "mentor");

        await RecordDownloadAsync(fixture.FirstMediaId, studentId, fixture.MaterialId);
        await RecordDownloadAsync(fixture.FirstMediaId, studentId, fixture.MaterialId);
        await RecordDownloadAsync(fixture.FirstMediaId, mentorId, fixture.MaterialId);

        var resp = await client
            .AsTeacher()
            .GetAsync($"/course/material/{fixture.MaterialId}/file-downloads/summary");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = ItemsOf(await resp.Content.ReadAsStringAsync());
        items.Should().HaveCount(2);

        var first = items.Single(i =>
            i.GetProperty("media").GetProperty("id").GetString() == fixture.FirstMediaId.ToString()
        );
        first.GetProperty("totalDownloads").GetInt32().Should().Be(3);
        first.GetProperty("uniqueUsers").GetInt32().Should().Be(2);

        // A file nobody has touched still has to appear, at zero.
        var second = items.Single(i =>
            i.GetProperty("media").GetProperty("id").GetString() == fixture.SecondMediaId.ToString()
        );
        second.GetProperty("totalDownloads").GetInt32().Should().Be(0);
        second.GetProperty("lastDownloadAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact(DisplayName = "GET material file-downloads breaks the downloads down per user")]
    public async Task Downloaders_AsTeacher_AggregatePerUser()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();

        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        await RecordDownloadAsync(fixture.FirstMediaId, studentId, fixture.MaterialId);
        await RecordDownloadAsync(fixture.FirstMediaId, studentId, fixture.MaterialId);
        await RecordDownloadAsync(fixture.SecondMediaId, studentId, fixture.MaterialId);

        var resp = await client
            .AsTeacher()
            .GetAsync($"/course/material/{fixture.MaterialId}/file-downloads");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        var items = ItemsOf(body);

        items.Should().HaveCount(1);
        items[0].GetProperty("downloadCount").GetInt32().Should().Be(3);
        items[0].GetProperty("user").GetProperty("id").GetString().Should().Be(studentId.ToString());

        JsonDocument
            .Parse(body)
            .RootElement.GetProperty("meta")
            .GetProperty("total")
            .GetInt32()
            .Should()
            .Be(1);
    }

    [Fact(DisplayName = "GET material file-downloads narrows to one file with mediaId")]
    public async Task Downloaders_CanBeNarrowedToOneFile()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();

        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        await RecordDownloadAsync(fixture.FirstMediaId, studentId, fixture.MaterialId);
        await RecordDownloadAsync(fixture.SecondMediaId, studentId, fixture.MaterialId);

        var resp = await client
            .AsTeacher()
            .GetAsync(
                $"/course/material/{fixture.MaterialId}/file-downloads?mediaId={fixture.SecondMediaId}"
            );
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = ItemsOf(await resp.Content.ReadAsStringAsync());
        items.Should().HaveCount(1);
        items[0].GetProperty("downloadCount").GetInt32().Should().Be(1);
    }

    [Fact(DisplayName = "GET material file-downloads as student returns 403")]
    public async Task Downloaders_AsStudent_IsForbidden()
    {
        using var client = _factory.CreateClient();
        var fixture = await SeedMaterialAsync();

        var summary = await client
            .AsStudent()
            .GetAsync($"/course/material/{fixture.MaterialId}/file-downloads/summary");
        summary.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var downloaders = await client
            .AsStudent()
            .GetAsync($"/course/material/{fixture.MaterialId}/file-downloads");
        downloaders.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET summary for an unknown material returns 404")]
    public async Task Summary_ForUnknownMaterial_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var resp = await client
            .AsTeacher()
            .GetAsync($"/course/material/{Ulid.NewUlid()}/file-downloads/summary");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
