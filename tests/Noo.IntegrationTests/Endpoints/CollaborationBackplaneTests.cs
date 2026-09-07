using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
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
/// The cases every single-instance test passes for the wrong reason. On one instance a lease is
/// visible because the same process took it, and an edit is relayed because the same hub holds
/// both sockets. In the cluster neither is true, so this is the only place the shared store and
/// the backplane are actually load-bearing.
/// </summary>
public class CollaborationBackplaneTests
{
    private static readonly JsonSerializerOptions _json = new JsonSerializerOptions(
        JsonSerializerDefaults.Web
    ).AddNooConverters();

    /// <summary>
    /// One database and one room store shared by both instances, the way two pods share MySQL and
    /// Redis; only the process, its hub lifetime manager and its connection store are separate.
    /// </summary>
    private sealed class ClusterApiFactory : ApiFactory
    {
        private readonly string _channelPrefix;

        public ClusterApiFactory(string databaseName, string channelPrefix)
        {
            DatabaseName = databaseName;
            _channelPrefix = channelPrefix;
        }

        protected override string DatabaseName { get; }

        protected override bool UseInMemoryCollaborationStore => false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            // UseSetting rather than ConfigureAppConfiguration: both sections are read eagerly
            // while Program.cs is still registering services.
            builder
                .UseSetting(
                    "Realtime:BackplaneConnectionString",
                    $"{BackplaneFactAttribute.Host}:{BackplaneFactAttribute.Port}"
                )
                .UseSetting("Realtime:ChannelPrefix", _channelPrefix)
                .UseSetting("Collaboration:ConnectionString", "127.0.0.1:6379");
        }
    }

    private static HubConnection ConnectTo(ApiFactory instance, Ulid userId)
    {
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Teacher, userId);

        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(instance.Server.BaseAddress, "hubs/collaboration"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => instance.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            )
            .AddJsonProtocol(options => options.PayloadSerializerOptions.AddNooConverters())
            .Build();
    }

    private static async Task<Ulid> CreateWorkAsync(ApiFactory instance)
    {
        using var client = instance.CreateClient();

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
                            maxScore = 5,
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

    [BackplaneFact]
    public async Task ALeaseTakenOnOneInstanceReachesAnEditorOnTheOther()
    {
        var databaseName = $"TestDb-{Guid.NewGuid()}";
        var channelPrefix = $"noo:rt:test:{Ulid.NewUlid()}:";

        await using var first = new ClusterApiFactory(databaseName, channelPrefix);
        await using var second = new ClusterApiFactory(databaseName, channelPrefix);

        var workId = await CreateWorkAsync(first);

        await using var anna = ConnectTo(first, Ulid.NewUlid());
        await using var boris = ConnectTo(second, Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        var borisState = await boris.InvokeAsync<CollaborationRoomState>(
            "JoinAsync",
            "work",
            workId
        );

        // The shared store is doing the work here: Anna is not connected to Boris's instance.
        borisState.Members.Should().HaveCount(2);

        var announced = new TaskCompletionSource<CollaborationLease>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        boris.On<CollaborationLease>(
            nameof(ICollaborationHubClient.LeaseChangedAsync),
            lease => announced.TrySetResult(lease)
        );

        await anna.InvokeAsync<CollaborationLease>("AcquireLeaseAsync", "/title");

        (await announced.Task.WaitAsync(TimeSpan.FromSeconds(20))).Path.Should().Be("/title");

        // And the lease is enforced across instances, not merely displayed.
        var refused = await Assert.ThrowsAnyAsync<Exception>(
            () =>
                boris.InvokeAsync<long>(
                    "PushOpsAsync",
                    new[]
                    {
                        new CollaborationOp
                        {
                            Op = "replace",
                            Path = "/title",
                            Value = JsonValue.Create("Борис"),
                        },
                    }
                )
        );
        refused.Message.Should().Contain("/title");
    }

    [BackplaneFact]
    public async Task AnEditPushedOnOneInstanceReachesTheOther()
    {
        var databaseName = $"TestDb-{Guid.NewGuid()}";
        var channelPrefix = $"noo:rt:test:{Ulid.NewUlid()}:";

        await using var first = new ClusterApiFactory(databaseName, channelPrefix);
        await using var second = new ClusterApiFactory(databaseName, channelPrefix);

        var workId = await CreateWorkAsync(first);

        await using var anna = ConnectTo(first, Ulid.NewUlid());
        await using var boris = ConnectTo(second, Ulid.NewUlid());
        await anna.StartAsync();
        await boris.StartAsync();

        await anna.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);
        await boris.InvokeAsync<CollaborationRoomState>("JoinAsync", "work", workId);

        var applied = new TaskCompletionSource<CollaborationOp[]>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        boris.On<long, CollaborationOp[]>(
            nameof(ICollaborationHubClient.OpsAppliedAsync),
            (_, ops) => applied.TrySetResult(ops)
        );

        await anna.InvokeAsync<long>(
            "PushOpsAsync",
            new[]
            {
                new CollaborationOp
                {
                    Op = "replace",
                    Path = "/title",
                    Value = JsonValue.Create("Через бэкплейн"),
                },
            }
        );

        var ops = await applied.Task.WaitAsync(TimeSpan.FromSeconds(20));
        ops.Single().Value!.GetValue<string>().Should().Be("Через бэкплейн");
    }
}
