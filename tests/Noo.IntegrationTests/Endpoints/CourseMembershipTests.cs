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
/// A student holds one membership per course and no more. The unique index behind that
/// is not exercised here — these tests run on EF InMemory, which does not enforce one —
/// so what they pin is the behaviour the API promises on top of it.
/// </summary>
public class CourseMembershipTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public CourseMembershipTests(ApiFactory factory)
    {
        _factory = factory;
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

    private static Task<HttpResponseMessage> AddMemberAsync(
        HttpClient client,
        Ulid courseId,
        Ulid studentId
    )
    {
        return client
            .AsAdmin()
            .PostAsJsonAsync(
                "/course/membership",
                new { courseId = courseId.ToString(), studentId = studentId.ToString() },
                JsonOptions
            );
    }

    private static async Task<Ulid> ReadIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(
            JsonOptions
        );

        return body!.Data!.Id;
    }

    private int MembershipCount(Ulid courseId, Ulid studentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();

        return db.GetDbSet<CourseMembershipModel>()
            .Count(m => m.CourseId == courseId && m.StudentId == studentId);
    }

    [Fact(DisplayName = "adding a student who is already on the course is a conflict")]
    public async Task Adding_The_Same_Student_Twice_Is_Refused()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        var first = await AddMemberAsync(client, courseId, studentId);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await AddMemberAsync(client, courseId, studentId);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        MembershipCount(courseId, studentId).Should().Be(1);
    }

    [Fact(DisplayName = "putting a removed student back revives the membership they had")]
    public async Task Re_Adding_A_Removed_Student_Revives_Their_Membership()
    {
        using var client = _factory.CreateClient();
        var courseId = await CreateCourseAsync();
        var studentId = TestDataHelpers.GetUserId(_factory, "student");

        var originalId = await ReadIdAsync(await AddMemberAsync(client, courseId, studentId));

        var removed = await client.AsAdmin().DeleteAsync($"/course/membership/{originalId}");
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var again = await AddMemberAsync(client, courseId, studentId);

        again.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadIdAsync(again)).Should().Be(originalId);
        MembershipCount(courseId, studentId).Should().Be(1);
    }
}
