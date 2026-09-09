using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Subjects.DTO;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// A course through the same room every document uses. Worth its own coverage because a course is
/// the case that tests the seam: one flat draft on the wire, two patch pipelines behind it, and a
/// content row that only comes into existence when somebody first types into a material.
/// </summary>
public class CourseCollaborationTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions _json = new JsonSerializerOptions(
        JsonSerializerDefaults.Web
    ).AddNooConverters();

    private readonly ApiFactory _factory;

    public CourseCollaborationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HubConnection BuildConnection(Ulid userId)
    {
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Teacher, userId);

        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress, "hubs/collaboration"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            )
            .AddJsonProtocol(options => options.PayloadSerializerOptions.AddNooConverters())
            .Build();
    }

    private async Task<(Ulid CourseId, Ulid ChapterId, Ulid MaterialId)> CreateCourseAsync(
        HttpClient client
    )
    {
        var subject = await client.AsAdmin()
            .PostAsJsonAsync(
                "/subject",
                new SubjectCreationDTO { Name = $"Subj-{Guid.NewGuid():N}", Color = "#00AAFF" },
                _json
            );
        subject.StatusCode.Should().Be(HttpStatusCode.Created);

        var subjectId = (
            await subject.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(_json)
        )!.Data!.Id;

        var course = await client.AsTeacher()
            .PostAsJsonAsync(
                "/course",
                new
                {
                    name = $"Course-{Guid.NewGuid():N}",
                    subjectId = subjectId.ToString(),
                    chapters = new[]
                    {
                        new
                        {
                            title = "Глава",
                            order = 1,
                            isActive = true,
                            materials = new[]
                            {
                                new
                                {
                                    title = "Материал",
                                    order = 1,
                                    isActive = true,
                                },
                            },
                        },
                    },
                },
                _json
            );
        course.StatusCode.Should().Be(HttpStatusCode.Created);

        var courseId = (
            await course.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(_json)
        )!.Data!.Id;

        var loaded = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var chapter = JsonDocument
            .Parse(await loaded.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data")
            .GetProperty("chapters")[0];

        return (
            courseId,
            Ulid.Parse(chapter.GetProperty("id").GetString()!),
            Ulid.Parse(chapter.GetProperty("materials")[0].GetProperty("id").GetString()!)
        );
    }

    private static CollaborationOp Replace(string path, object? value) =>
        new()
        {
            Op = "replace",
            Path = path,
            Value = System.Text.Json.Nodes.JsonNode.Parse(
                JsonSerializer.Serialize(value, _json)
            ),
        };

    // Both halves of a course in one draft, saved through the endpoints every document shares.
    [Fact(DisplayName = "Course collaboration: one draft saves the tree and a material's content")]
    public async Task SavingACourseDraftWritesBothPipelines()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId, materialId) = await CreateCourseAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "course", courseId);

        var seq = await anna.InvokeAsync<long>(
            "PushOpsAsync",
            new[]
            {
                Replace("/name", "Переименован"),
                Replace($"/chapters/{chapterId}/title", "Новая глава"),
                Replace(
                    $"/chapters/{chapterId}/materials/{materialId}/content/content",
                    System.Text.Json.Nodes.JsonNode.Parse(
                        """
                        {"$type":"tiptap","type":"doc","content":[
                          {"type":"paragraph","content":[{"type":"text","text":"Текст"}]}]}
                        """
                    )
                ),
            }
        );

        var response = await client.AsTeacher()
            .PostAsJsonAsync(
                $"/collaboration/course/{courseId}/save",
                new CollaborationSaveRequestDTO { ExpectedSeq = seq },
                _json
            );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var data = JsonDocument
            .Parse(await saved.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data");

        data.GetProperty("name").GetString().Should().Be("Переименован");

        var chapter = data.GetProperty("chapters")[0];
        chapter.GetProperty("title").GetString().Should().Be("Новая глава");

        // The content row did not exist when the draft was made; the save is what created it.
        chapter
            .GetProperty("materials")[0]
            .GetProperty("contentId")
            .GetString()
            .Should()
            .NotBeNull();
    }

    [Fact(DisplayName = "Course collaboration: a path the course patch would reject is refused")]
    public async Task UneditablePathsAreRefused()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId, materialId) = await CreateCourseAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "course", courseId);

        // The server owns this one: pointing a material at another's content would reach a row
        // the caller may not be able to open at all.
        await Assert.ThrowsAsync<HubException>(
            () =>
                anna.InvokeAsync<long>(
                    "PushOpsAsync",
                    new[]
                    {
                        Replace(
                            $"/chapters/{chapterId}/materials/{materialId}/contentId",
                            Ulid.NewUlid().ToString()
                        ),
                    }
                )
        );
    }
}
