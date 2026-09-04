using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Realtime.Ping;

namespace Noo.UnitTests.Core.Realtime;

public class SignalRRealtimePublisherTests
{
    private sealed class RecordingClients : IHubClients<IRealtimePingClient>
    {
        public List<string[]> UserBatches { get; } = [];
        public bool SentToAll { get; private set; }
        public List<string> GroupNames { get; } = [];

        private static IRealtimePingClient Proxy => new Mock<IRealtimePingClient>().Object;

        public IRealtimePingClient All
        {
            get
            {
                SentToAll = true;
                return Proxy;
            }
        }

        public IRealtimePingClient User(string userId)
        {
            UserBatches.Add([userId]);
            return Proxy;
        }

        public IRealtimePingClient Users(IReadOnlyList<string> userIds)
        {
            UserBatches.Add([.. userIds]);
            return Proxy;
        }

        public IRealtimePingClient Group(string groupName)
        {
            GroupNames.Add(groupName);
            return Proxy;
        }

        public IRealtimePingClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IRealtimePingClient Client(string connectionId) => Proxy;
        public IRealtimePingClient Clients(IReadOnlyList<string> connectionIds) => Proxy;
        public IRealtimePingClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IRealtimePingClient Groups(IReadOnlyList<string> groupNames) => Proxy;
    }

    private sealed class StubHubContext : IHubContext<RealtimePingHub, IRealtimePingClient>
    {
        public StubHubContext(RecordingClients clients)
        {
            Clients = clients;
        }

        public IHubClients<IRealtimePingClient> Clients { get; }

        public IGroupManager Groups => new Mock<IGroupManager>().Object;
    }

    private static (SignalRRealtimePublisher<RealtimePingHub, IRealtimePingClient> publisher, RecordingClients clients) Create(
        int chunkSize = 500
    )
    {
        var clients = new RecordingClients();
        var config = Options.Create(
            new RealtimeConfig { BroadcastChunkSize = chunkSize, BroadcastChunkDelayMs = 0 }
        );

        var publisher = new SignalRRealtimePublisher<RealtimePingHub, IRealtimePingClient>(
            new StubHubContext(clients),
            new RealtimeMetrics(new DummyMeterFactory()),
            config
        );

        return (publisher, clients);
    }

    private static Task Noop(IRealtimePingClient client) => Task.CompletedTask;

    [Fact]
    public async Task SendsToOneUserByIdentifier()
    {
        var (publisher, clients) = Create();
        var userId = Ulid.NewUlid();

        await publisher.SendToUserAsync(userId, Noop);

        Assert.Equal([userId.ToString()], Assert.Single(clients.UserBatches));
    }

    // A single call naming every user costs one backplane publish per user, so it is split.
    [Fact]
    public async Task SplitsALargeUserListIntoChunks()
    {
        var (publisher, clients) = Create(chunkSize: 100);
        var userIds = Enumerable.Range(0, 250).Select(_ => Ulid.NewUlid()).ToArray();

        await publisher.SendToUsersAsync(userIds, Noop);

        Assert.Equal(3, clients.UserBatches.Count);
        Assert.Equal([100, 100, 50], clients.UserBatches.Select(b => b.Length));
        Assert.Equal(
            userIds.Select(id => id.ToString()),
            clients.UserBatches.SelectMany(b => b)
        );
    }

    [Fact]
    public async Task SendsNothingForAnEmptyUserList()
    {
        var (publisher, clients) = Create();

        await publisher.SendToUsersAsync([], Noop);

        Assert.Empty(clients.UserBatches);
    }

    // Reaching everyone is one publish that each instance fans out locally, so unlike an
    // enumerated user list it must not be chunked.
    [Fact]
    public async Task BroadcastsInOneCallRatherThanEnumeratingRecipients()
    {
        var (publisher, clients) = Create(chunkSize: 1);

        await publisher.BroadcastAsync(Noop);

        Assert.True(clients.SentToAll);
        Assert.Empty(clients.UserBatches);
    }

    [Fact]
    public async Task SendsToAGroupByName()
    {
        var (publisher, clients) = Create();

        await publisher.SendToGroupAsync("course:42", Noop);

        Assert.Equal("course:42", Assert.Single(clients.GroupNames));
    }

    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose()
        {
        }
    }
}
