using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Subjects.DTO;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Regression coverage for the "own courses" tab: POST /course must store the
/// creator as a course author, and GET /course?authorId= must return only the
/// courses authored by that user.
/// </summary>
public class CourseAuthorTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CourseAuthorTests(ApiFactory factory)
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

    private static async Task<Ulid> CreateCourseAsync(HttpClient client, Ulid creatorId)
    {
        var subjectId = await CreateSubjectAsync(client);

        var payload = new
        {
            name = $"Course-{Guid.NewGuid():N}",
            subjectId = subjectId.ToString(),
            chapters = Array.Empty<object>(),
        };

        var resp = await client.AsTeacher(creatorId).PostAsJsonAsync("/course", payload, JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(JsonOptions);
        return body!.Data!.Id;
    }

    private static async Task<List<string?>> SearchCourseIdsAsync(HttpClient client, string query)
    {
        var resp = await client.GetAsync($"/course{query}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").EnumerateArray()
            .Select(c => c.GetProperty("id").GetString())
            .ToList();
    }

    [Fact(DisplayName = "POST /course stores the creator as course author")]
    public async Task Post_Course_Stores_Creator_As_Author()
    {
        using var client = _factory.CreateClient();
        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");

        var courseId = await CreateCourseAsync(client, teacherId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Noo.Api.Core.DataAbstraction.Db.NooDbContext>();
        var course = await db
            .GetDbSet<Noo.Api.Courses.Models.CourseModel>()
            .Include(c => c.Authors)
            .FirstAsync(c => c.Id == courseId);

        course.Authors.Should().ContainSingle(a => a.Id == teacherId);
    }

    [Fact(DisplayName = "GET /course?authorId= returns only the courses authored by that user (own courses tab)")]
    public async Task Get_Courses_Filtered_By_AuthorId_Returns_Only_Own_Courses()
    {
        using var client = _factory.CreateClient();
        var teacherId = TestDataHelpers.GetUserId(_factory, "teacher");
        var otherTeacherId = await TestDataHelpers.CreateUserAsync(
            _factory,
            $"teacher-{Guid.NewGuid():N}"[..20],
            $"other-{Guid.NewGuid():N}@example.com",
            "Password123!",
            UserRoles.Teacher);

        var ownCourseId = await CreateCourseAsync(client, teacherId);
        var foreignCourseId = await CreateCourseAsync(client, otherTeacherId);

        var ownIds = await SearchCourseIdsAsync(
            client.AsTeacher(teacherId), $"?authorId={teacherId}");

        ownIds.Should().Contain(ownCourseId.ToString(),
            because: "the creator must see their course on the own courses tab");
        ownIds.Should().NotContain(foreignCourseId.ToString(),
            because: "courses authored by other users must not appear on the own courses tab");

        var unfiltered = await SearchCourseIdsAsync(client.AsTeacher(teacherId), "");
        unfiltered.Should().Contain(ownCourseId.ToString());
        unfiltered.Should().Contain(foreignCourseId.ToString());
    }
}
