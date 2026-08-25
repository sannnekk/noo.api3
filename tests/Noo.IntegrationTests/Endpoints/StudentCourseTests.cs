using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Response;
using Noo.Api.Courses.Models;
using Noo.Api.Subjects.DTO;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// A publicly open course must behave for a student exactly like an assigned one — it shows up in
/// their list, pins and archives — while costing one audience row rather than one membership row
/// per student. Flipping the course back to private must therefore delete nothing.
/// </summary>
public class StudentCourseTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Pinned rather than <c>AsStudent()</c>, which mints a fresh random id on every call: these
    /// tests write per-student state and read it back, so both calls must be the same student.
    /// </summary>
    private readonly Ulid _studentId;

    public StudentCourseTests(ApiFactory factory)
    {
        _factory = factory;
        _studentId = TestDataHelpers.GetUserId(factory, "student");
    }

    private async Task<Ulid> CreateCourseAsync()
    {
        using var client = _factory.CreateClient();

        var subject = await client
            .AsAdmin()
            .PostAsJsonAsync(
                "/subject",
                new SubjectCreationDTO { Name = $"Subj-{Guid.NewGuid():N}", Color = "#00AAFF" },
                JsonOptions
            );
        var subjectId = (
            await subject.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions)
        )!.Data!.Id;

        var course = await client
            .AsTeacher()
            .PostAsJsonAsync(
                "/course",
                new
                {
                    name = $"Course-{Guid.NewGuid():N}",
                    subjectId = subjectId.ToString(),
                    chapters = Array.Empty<object>(),
                },
                JsonOptions
            );

        return (
            await course.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions)
        )!.Data!.Id;
    }

    private static Task<HttpResponseMessage> SetPublicAsync(
        HttpClient client,
        Ulid courseId,
        bool isPublic
    )
    {
        var patch = new[]
        {
            new
            {
                op = "replace",
                path = "/isPublic",
                value = isPublic,
            },
        };

        return client.AsTeacher().PatchAsJsonAsync($"/course/{courseId}", patch, JsonOptions);
    }

    private Task<HttpResponseMessage> PatchMyStateAsync(
        HttpClient client,
        Ulid courseId,
        string path,
        bool value
    )
    {
        var patch = new[]
        {
            new
            {
                op = "replace",
                path,
                value,
            },
        };

        return client
            .AsUserId(_studentId)
            .PatchAsJsonAsync($"/course/{courseId}/my-state", patch, JsonOptions);
    }

    private async Task<JsonElement[]> GetOwnCoursesAsync(HttpClient client, bool archived = false)
    {
        var response = await client
            .AsUserId(_studentId)
            .GetAsync($"/course/my?page=1&perPage=50&isArchived={archived.ToString().ToLower()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return body.GetProperty("data").EnumerateArray().ToArray();
    }

    private static JsonElement? Find(JsonElement[] items, Ulid courseId)
    {
        foreach (var item in items)
        {
            if (item.GetProperty("id").GetString() == courseId.ToString())
            {
                return item;
            }
        }

        return null;
    }

    private int AudienceCount(Ulid courseId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        return db.GetDbSet<CourseAudienceModel>().Count(a => a.CourseId == courseId);
    }

    private int MembershipCount(Ulid courseId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        return db.GetDbSet<CourseMembershipModel>().Count(m => m.CourseId == courseId);
    }

    private int StudentStateCount(Ulid courseId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        return db.GetDbSet<CourseStudentStateModel>().Count(s => s.CourseId == courseId);
    }

    [Fact(DisplayName = "GET /course/my omits a course the student was never given")]
    public async Task An_Unrelated_Course_Is_Not_Listed()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();

        Find(await GetOwnCoursesAsync(client), courseId).Should().BeNull();
    }

    [Fact(DisplayName = "making a course public lists it for a student with no membership row")]
    public async Task A_Public_Course_Is_Listed_Without_A_Membership()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();

        (await SetPublicAsync(client, courseId, true)).StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        var listed = Find(await GetOwnCoursesAsync(client), courseId);

        listed.Should().NotBeNull();
        listed!.Value.GetProperty("accessSource").GetString().Should().Be("public");

        // The whole point: one audience row, and not a single membership row was written.
        AudienceCount(courseId).Should().Be(1);
        MembershipCount(courseId).Should().Be(0);
    }

    [Fact(DisplayName = "a student can open a public course they were never assigned to")]
    public async Task A_Public_Course_Can_Be_Opened()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();

        (await client.AsUserId(_studentId).GetAsync($"/course/{courseId}")).StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);

        await SetPublicAsync(client, courseId, true);

        (await client.AsUserId(_studentId).GetAsync($"/course/{courseId}")).StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "a student can pin and archive a public course")]
    public async Task A_Public_Course_Can_Be_Pinned_And_Archived()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();
        await SetPublicAsync(client, courseId, true);

        var pinned = await PatchMyStateAsync(client, courseId, "/isPinned", true);
        pinned.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listed = Find(await GetOwnCoursesAsync(client), courseId);
        listed!.Value.GetProperty("isPinned").GetBoolean().Should().BeTrue();

        var archived = await PatchMyStateAsync(client, courseId, "/isArchived", true);
        archived.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Find(await GetOwnCoursesAsync(client), courseId).Should().BeNull();

        var inArchive = Find(await GetOwnCoursesAsync(client, archived: true), courseId);
        inArchive.Should().NotBeNull();
        // Pinning survives archiving — the two flags are independent.
        inArchive!.Value.GetProperty("isPinned").GetBoolean().Should().BeTrue();
    }

    [Fact(DisplayName = "a student cannot pin a course they have no access to")]
    public async Task Pinning_An_Unreachable_Course_Is_Forbidden()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();

        var response = await PatchMyStateAsync(client, courseId, "/isPinned", true);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "making a course private again revokes access without deleting any rows")]
    public async Task Unpublishing_Revokes_Access_And_Deletes_Nothing()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();
        await SetPublicAsync(client, courseId, true);

        await PatchMyStateAsync(client, courseId, "/isPinned", true);
        StudentStateCount(courseId).Should().Be(1);

        (await SetPublicAsync(client, courseId, false)).StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        Find(await GetOwnCoursesAsync(client), courseId).Should().BeNull();
        (await client.AsUserId(_studentId).GetAsync($"/course/{courseId}")).StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);

        // Going private removes the one audience row and leaves per-student state untouched:
        // that state carries no access, so there is nothing to clean up.
        AudienceCount(courseId).Should().Be(0);
        StudentStateCount(courseId).Should().Be(1);
    }

    [Fact(DisplayName = "an assigned course is listed with its assignment metadata")]
    public async Task An_Assigned_Course_Reports_Its_Membership()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        var added = await client
            .AsAdmin()
            .PostAsJsonAsync(
                "/course/membership",
                new { courseId = courseId.ToString(), studentId = studentId.ToString() },
                JsonOptions
            );
        added.StatusCode.Should().Be(HttpStatusCode.Created);

        var listed = Find(await GetOwnCoursesAsync(client), courseId);

        listed.Should().NotBeNull();
        listed!.Value.GetProperty("accessSource").GetString().Should().Be("assignment");
        listed.Value.GetProperty("membershipType").GetString().Should().Be("manual-assigned");
    }
}
