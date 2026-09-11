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
using Noo.Api.Core.System.Collaboration.Realtime;
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

    private static TaskCompletionSource<T> Expect<T>(HubConnection connection, string method)
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On<T>(method, value => source.TrySetResult(value));

        return source;
    }

    /// <summary>
    /// Waits for a push that satisfies <paramref name="until"/>, not merely the first one. A
    /// joining editor is told about its own arrival before anyone else's, so latching the first
    /// roster would assert on the wrong one.
    /// </summary>
    private static TaskCompletionSource<T> ExpectWhere<T>(
        HubConnection connection,
        string method,
        Func<T, bool> until
    )
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On<T>(
            method,
            value =>
            {
                if (until(value))
                {
                    source.TrySetResult(value);
                }
            }
        );

        return source;
    }

    private static Task<T> Within<T>(TaskCompletionSource<T> source) =>
        source.Task.WaitAsync(TimeSpan.FromSeconds(15));

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

    // Two people in one course, which is the whole feature and what none of the coverage so far
    // actually exercised: that each of them appears in the other's roster, that an edit crosses,
    // and that a caret frame reaches the other editor of the same material.
    [Fact(DisplayName = "Course collaboration: two editors see each other, each other's edits and each other's carets")]
    public async Task TwoEditorsShareTheRoom()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId, materialId) = await CreateCourseAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        var annaSawPresence = ExpectWhere<CollaborationMember[]>(
            anna,
            nameof(ICollaborationHubClient.PresenceChangedAsync),
            members => members.Length == 2
        );

        var annaState = await anna.InvokeAsync<CollaborationRoomState>(
            "JoinAsync",
            "course",
            courseId
        );
        var borisState = await boris.InvokeAsync<CollaborationRoomState>(
            "JoinAsync",
            "course",
            courseId
        );

        // The roster the second arrival is handed already holds the first.
        borisState.Members.Should().HaveCount(2);
        annaState.Members.Should().HaveCount(1);

        // And the first is told about the second without asking.
        (await Within(annaSawPresence)).Should().HaveCount(2);

        // Each of them is told which member of the roster is themselves, so neither has to
        // recognise itself — an editor that guesses wrong lists itself as somebody else.
        annaState.SelfConnectionId.Should().Be(annaState.Members.Single().ConnectionId);
        borisState.SelfConnectionId.Should().NotBeNullOrEmpty();
        borisState
            .Members.Select(member => member.ConnectionId)
            .Should()
            .Contain([annaState.SelfConnectionId, borisState.SelfConnectionId]);
        borisState.SelfConnectionId.Should().NotBe(annaState.SelfConnectionId);

        var applied = new TaskCompletionSource<(long, CollaborationOp[])>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        boris.On<long, CollaborationOp[]>(
            nameof(ICollaborationHubClient.OpsAppliedAsync),
            (fromSeq, ops) => applied.TrySetResult((fromSeq, ops))
        );

        await anna.InvokeAsync<long>(
            "PushOpsAsync",
            new[] { Replace("/name", "Переименован") }
        );

        var (_, crossed) = await applied.Task.WaitAsync(TimeSpan.FromSeconds(15));
        crossed.Single().Path.Should().Be("/name");

        // The caret channel: one scope per material, and a frame reaches the others in it.
        var scope = $"material:{materialId}";

        await anna.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", scope);
        await boris.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", scope);

        var borisSawFrame = Expect<CollaborationFrame>(
            boris,
            nameof(ICollaborationHubClient.YjsFrameAsync)
        );

        await anna.InvokeAsync(
            "PushYjsAsync",
            new CollaborationFrame
            {
                Scope = scope,
                Kind = "y1",
                Payload = "AQIDBA==",
            }
        );

        (await Within(borisSawFrame)).Payload.Should().Be("AQIDBA==");
    }

    // Exactly one client is told to write the starting content into a fresh shared document; two
    // seeding one document is how a collaborative editor ends up showing everything twice.
    [Fact(DisplayName = "Course collaboration: one editor of a material is elected to seed its text")]
    public async Task OneEditorSeedsAMaterialsText()
    {
        using var client = _factory.CreateClient();
        var (courseId, _, materialId) = await CreateCourseAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "course", courseId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "course", courseId);

        var scope = $"material:{materialId}";
        var first = await anna.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", scope);
        var second = await boris.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", scope);

        first.MaySeed.Should().BeTrue();
        second.MaySeed.Should().BeFalse();
        second.MemberCount.Should().Be(2);
    }

    // A material is drafted inactive, so an editor handed the reader's filtered tree would be
    // missing everything anyone had just added — and would be editing a document it cannot see.
    [Fact(DisplayName = "Course collaboration: an editor is handed the whole tree, published or not")]
    public async Task AnEditorSeesUnpublishedMaterials()
    {
        using var client = _factory.CreateClient();
        var (courseId, chapterId, _) = await CreateCourseAsync(client);
        var draftedId = Ulid.NewUlid();

        await using var anna = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "course", courseId);

        var seq = await anna.InvokeAsync<long>(
            "PushOpsAsync",
            new[]
            {
                new CollaborationOp
                {
                    Op = "add",
                    Path = $"/chapters/{chapterId}/materials/{draftedId}",
                    Value = System.Text.Json.Nodes.JsonNode.Parse(
                        $$"""
                        {"id":"{{draftedId}}","order":2,"title":"Черновик","isActive":false,
                         "isPinned":false}
                        """
                    ),
                },
            }
        );

        var response = await client.AsTeacher()
            .PostAsJsonAsync(
                $"/collaboration/course/{courseId}/save",
                new CollaborationSaveRequestDTO { ExpectedSeq = seq },
                _json
            );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var reloaded = await client.AsTeacher().GetAsync($"/course/{courseId}");
        var materials = JsonDocument
            .Parse(await reloaded.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data")
            .GetProperty("chapters")[0]
            .GetProperty("materials");

        // The one that was there plus the unpublished one somebody just drafted.
        materials.GetArrayLength().Should().Be(2);
        materials
            .EnumerateArray()
            .Select(material => material.GetProperty("id").GetString())
            .Should()
            .Contain(draftedId.ToString());
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
