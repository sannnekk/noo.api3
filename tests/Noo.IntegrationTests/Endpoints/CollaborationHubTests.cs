using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
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
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Types;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// Two editors on one work, which is the whole feature: what one of them claims the other must
/// see claimed, what one of them changes the other must see change, and neither may lose an edit
/// to the other pressing Save.
/// </summary>
public class CollaborationHubTests : IClassFixture<ApiFactory>
{
    /// <summary>
    /// The app's own wire conventions — hyphen-lower-case enums and Moscow time. The TypeScript
    /// client gets them for free; a .NET one has to be told, on both transports.
    /// </summary>
    private static readonly JsonSerializerOptions _json = new JsonSerializerOptions(
        JsonSerializerDefaults.Web
    ).AddNooConverters();

    private readonly ApiFactory _factory;

    public CollaborationHubTests(ApiFactory factory)
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

    private async Task<Ulid> CreateWorkAsync(HttpClient client, int maxScore = 5)
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

        var work = await client.AsTeacher()
            .PostAsJsonAsync(
                "/work",
                new
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
                            maxScore,
                            content = RichTextFactory.Create("Q1"),
                        },
                    },
                },
                _json
            );
        work.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await work.Content.ReadFromJsonAsync<ApiResponseDTO<IdResponseDTO>>(_json))!
            .Data!
            .Id;
    }

    private static TaskCompletionSource<T> Expect<T>(HubConnection connection, string method)
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On<T>(method, value => source.TrySetResult(value));

        return source;
    }

    private static Task<T> Within<T>(TaskCompletionSource<T> source) =>
        source.Task.WaitAsync(TimeSpan.FromSeconds(15));

    // A lease taken by one editor has to reach the other unprompted — that push is what disables
    // the input, and without it the second teacher types into a field already being edited.
    [Fact(DisplayName = "Hub: a claimed field is announced to the rest of the room")]
    public async Task AcquiringALeaseReachesTheOtherEditor()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);

        var seen = Expect<CollaborationLease>(
            boris,
            nameof(ICollaborationHubClient.LeaseChangedAsync)
        );

        var lease = await anna.InvokeAsync<CollaborationLease>("AcquireLeaseAsync", "/title");
        lease.Holder.Should().NotBeNull();

        var announced = await Within(seen);
        announced.Path.Should().Be("/title");
        announced.Holder!.ConnectionId.Should().Be(lease.Holder!.ConnectionId);
    }

    // The server has to enforce the lease itself: an input that disables itself is being polite,
    // and a client that skipped the UI would otherwise overwrite the holder's edit.
    [Fact(DisplayName = "Hub: writing to a field someone else holds is refused")]
    public async Task WritingToALeasedFieldIsRefused()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await anna.InvokeAsync<CollaborationLease>("AcquireLeaseAsync", "/title");

        var refused = await Assert.ThrowsAsync<HubException>(
            () => boris.InvokeAsync<long>("PushOpsAsync", new[] { Op("/title", "Борис") })
        );
        refused.Message.Should().Contain("/title");

        // The holder itself is not blocked by its own lease.
        await anna.InvokeAsync<long>("PushOpsAsync", new[] { Op("/title", "Анна") });
    }

    [Fact(DisplayName = "Hub: an edit reaches the other editor with its sequence number")]
    public async Task PushedOpsReachTheOtherEditor()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);

        var applied = new TaskCompletionSource<(long, CollaborationOp[])>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        boris.On<long, CollaborationOp[]>(
            nameof(ICollaborationHubClient.OpsAppliedAsync),
            (fromSeq, ops) => applied.TrySetResult((fromSeq, ops))
        );

        var seq = await anna.InvokeAsync<long>(
            "PushOpsAsync",
            new[] { Op("/title", "Переписано") }
        );
        seq.Should().Be(1);

        var (fromSeq, ops) = await applied.Task.WaitAsync(TimeSpan.FromSeconds(15));
        fromSeq.Should().Be(1);
        ops.Single().Path.Should().Be("/title");
    }

    // A killed tab must not hold a field until its lease lapses; the disconnect is the fast path.
    [Fact(DisplayName = "Hub: a disconnecting editor frees the fields it held")]
    public async Task DisconnectingFreesHeldFields()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await anna.InvokeAsync<CollaborationLease>("AcquireLeaseAsync", "/title");

        var released = Expect<CollaborationLease>(
            boris,
            nameof(ICollaborationHubClient.LeaseChangedAsync)
        );

        await anna.DisposeAsync();

        (await Within(released)).Holder.Should().BeNull();

        // And the field really is writable again, not merely reported as free.
        await boris.InvokeAsync<long>("PushOpsAsync", new[] { Op("/title", "Борис") });
    }

    [Fact(DisplayName = "Hub: joining a scope elects exactly one client to seed it")]
    public async Task JoiningAScopeElectsOneSeeder()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);

        var first = await anna.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", "task-1");
        var second = await boris.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", "task-1");

        first.MaySeed.Should().BeTrue();
        second.MaySeed.Should().BeFalse();
        second.MemberCount.Should().Be(2);
    }

    // The relay is the whole rich-text story: the server passes the bytes on without decoding
    // them, which is what makes multiple carets work with no CRDT on this side.
    [Fact(DisplayName = "Hub: a CRDT frame reaches the others in its scope and not the sender")]
    public async Task YjsFramesAreRelayedWithinTheScope()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await using var clara = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();
        await clara.StartAsync();

        foreach (var connection in new[] { anna, boris, clara })
        {
            await connection.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        }

        await anna.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", "task-1");
        await boris.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", "task-1");
        await clara.InvokeAsync<CollaborationScopeState>("JoinScopeAsync", "task-2");

        var borisSaw = Expect<CollaborationFrame>(
            boris,
            nameof(ICollaborationHubClient.YjsFrameAsync)
        );
        var claraSaw = Expect<CollaborationFrame>(
            clara,
            nameof(ICollaborationHubClient.YjsFrameAsync)
        );
        var annaSaw = Expect<CollaborationFrame>(
            anna,
            nameof(ICollaborationHubClient.YjsFrameAsync)
        );

        await anna.InvokeAsync(
            "PushYjsAsync",
            new CollaborationFrame
            {
                Scope = "task-1",
                Kind = "sync",
                Payload = "AQIDBA==",
            }
        );

        (await Within(borisSaw)).Payload.Should().Be("AQIDBA==");

        // Clara is on another task and the sender already applied its own update locally.
        claraSaw.Task.IsCompleted.Should().BeFalse();
        annaSaw.Task.IsCompleted.Should().BeFalse();
    }

    [Fact(DisplayName = "Collaboration: saving the draft writes it through the work's patch pipeline")]
    public async Task SavingAppliesTheDraft()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client, maxScore: 5);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await using var boris = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);

        var seq = await anna.InvokeAsync<long>("PushOpsAsync", new[] { Op("/title", "Сохранено") });

        var notified = Expect<CollaborationSaved>(
            boris,
            nameof(ICollaborationHubClient.DocumentSavedAsync)
        );

        var response = await client.AsTeacher()
            .PostAsJsonAsync(
                $"/collaboration/work/{workId}/save",
                new CollaborationSaveRequestDTO { ExpectedSeq = seq },
                _json
            );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await Within(notified)).Version.Should().Be(1);

        var work = await client.AsTeacher().GetAsync($"/work/{workId}");
        var data = JsonDocument
            .Parse(await work.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("Сохранено");

        // The draft is gone, so a second save has nothing to apply.
        var again = await client.AsTeacher()
            .PostAsJsonAsync(
                $"/collaboration/work/{workId}/save",
                new CollaborationSaveRequestDTO { ExpectedSeq = 0 },
                _json
            );
        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Without this check, an operation still in flight when Save was pressed would be applied to
    // nothing and thrown away with the draft — an edit lost with no error anywhere.
    [Fact(DisplayName = "Collaboration: a save racing an unseen edit is refused rather than losing it")]
    public async Task SavingWithAStaleSequenceIsRefused()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await anna.InvokeAsync<long>("PushOpsAsync", new[] { Op("/title", "Первое") });
        await anna.InvokeAsync<long>("PushOpsAsync", new[] { Op("/description", "Второе") });

        var response = await client.AsTeacher()
            .PostAsJsonAsync(
                $"/collaboration/work/{workId}/save",
                new CollaborationSaveRequestDTO { ExpectedSeq = 1 },
                _json
            );

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Collaboration: the room state carries the draft, the roster and the leases")]
    public async Task RoomStateCarriesTheDraft()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await anna.InvokeAsync<long>("PushOpsAsync", new[] { Op("/title", "Черновик") });
        await anna.InvokeAsync<CollaborationLease>("AcquireLeaseAsync", "/description");

        var response = await client.AsTeacher().GetAsync($"/collaboration/work/{workId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var state = (
            await response.Content.ReadFromJsonAsync<ApiResponseDTO<CollaborationRoomState>>(_json)
        )!.Data!;

        state.Seq.Should().Be(1);
        state.Ops.Single().Path.Should().Be("/title");
        state.Members.Single().Role.Should().Be(UserRoles.Teacher);
        state.Leases.Single().Path.Should().Be("/description");
    }

    // Saving throws the draft away, so a save that does not go through must not: the work is
    // still what it was, and the edits have to stay where everyone can see them and fix them.
    // This covers a draft the entity's own validation refuses; a failure at commit time is
    // guarded by the explicit commit in the controller, which the InMemory provider cannot
    // provoke.
    [Fact(DisplayName = "Collaboration: a save the entity rejects leaves the draft intact")]
    public async Task ARejectedSaveKeepsTheDraft()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        await using var anna = BuildConnection(Ulid.NewUlid());
        await anna.StartAsync();
        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);

        var taskId = (await GetTaskIdAsync(client, workId)).ToString();

        // Past what the column can hold, which the DTO's own validation refuses.
        var seq = await anna.InvokeAsync<long>(
            "PushOpsAsync",
            new[]
            {
                new CollaborationOp
                {
                    Op = "replace",
                    Path = $"/tasks/{taskId}/maxScore",
                    Value = JsonValue.Create(9999)
                }
            }
        );

        var response = await client.AsTeacher()
            .PostAsJsonAsync(
                $"/collaboration/work/{workId}/save",
                new CollaborationSaveRequestDTO { ExpectedSeq = seq },
                _json
            );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var state = await client.AsTeacher().GetAsync($"/collaboration/work/{workId}");
        var room = (
            await state.Content.ReadFromJsonAsync<ApiResponseDTO<CollaborationRoomState>>(_json)
        )!.Data!;

        room.Ops.Should().HaveCount(1);
        room.Version.Should().Be(0);
    }

    private async Task<Ulid> GetTaskIdAsync(HttpClient client, Ulid workId)
    {
        var response = await client.AsTeacher().GetAsync($"/work/{workId}");
        var data = JsonDocument
            .Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data");

        return Ulid.Parse(
            data.GetProperty("tasks").EnumerateArray().Single().GetProperty("id").GetString()!
        );
    }

    [Fact(DisplayName = "Collaboration: a room type nobody registered is a bad request")]
    public async Task AnUnknownRoomTypeIsRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.AsTeacher()
            .GetAsync($"/collaboration/spaceship/{Ulid.NewUlid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Collaboration: a student may not open a room")]
    public async Task AStudentMayNotOpenARoom()
    {
        using var client = _factory.CreateClient();
        var workId = await CreateWorkAsync(client);

        var response = await client.AsStudent().GetAsync($"/collaboration/work/{workId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static CollaborationOp Op(string path, string value) =>
        new()
        {
            Op = "replace",
            Path = path,
            Value = JsonValue.Create(value),
        };
}
