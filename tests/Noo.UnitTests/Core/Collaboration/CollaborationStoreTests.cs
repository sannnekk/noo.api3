using System.Net.Sockets;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Collaboration;
using Noo.Api.Core.System.Collaboration.Store;
using Noo.Api.Core.Utils;
using StackExchange.Redis;

namespace Noo.UnitTests.Core.Collaboration;

/// <summary>
/// One suite, both stores. The in-memory store exists so a single instance and the test suite
/// need no Redis, which is only safe while it behaves identically — so the behaviour is written
/// once and each implementation is held to it.
///
/// Redis is a case rather than a skipped class: when it is not listening the case is simply not
/// yielded, so nothing reports green for a store it never ran. When it is, the Lua scripts get
/// exercised — take-only-if-free, release-only-if-mine, swap-only-if-unchanged is the whole
/// correctness story for leases across instances, and no substitute runs Lua.
/// </summary>
public class CollaborationStoreTests
{
    private const string RedisHost = "127.0.0.1";
    private const int RedisPort = 6379;

    private static readonly CollaborationConfig _config = new()
    {
        LeaseTtlSeconds = 20,
        PresenceTtlSeconds = 45,
        DraftTtlHours = 4,
    };

    private static readonly Lazy<IConnectionMultiplexer?> _redis = new(() =>
        IsRedisListening() ? ConnectionMultiplexer.Connect($"{RedisHost}:{RedisPort}") : null
    );

    public static TheoryData<string> Stores()
    {
        var stores = new TheoryData<string> { "memory" };

        if (_redis.Value is not null)
        {
            stores.Add("redis");
        }

        return stores;
    }

    private static ICollaborationStore Create(string kind)
    {
        var options = Options.Create(_config);

        return kind == "redis"
            ? new RedisCollaborationStore(_redis.Value!, options)
            : new InMemoryCollaborationStore(options);
    }

    /// <summary>A fresh room per test, so a run against a shared Redis never collides.</summary>
    private static CollaborationRoom NewRoom() => new("work", Ulid.NewUlid());

    private static CollaborationMember Member(string connectionId, string name = "Учитель") =>
        new()
        {
            ConnectionId = connectionId,
            UserId = Ulid.NewUlid(),
            Name = name,
            Role = UserRoles.Teacher,
            JoinedAt = Clock.Now,
        };

    private static CollaborationOp Op(string path, int value) =>
        new()
        {
            Op = "replace",
            Path = path,
            Value = JsonValue.Create(value),
        };

    [Theory(DisplayName = "Store: a joining member sees everyone already in the room")]
    [MemberData(nameof(Stores))]
    public async Task Join_ReturnsTheRoster(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();

        await store.JoinAsync(room, Member("a", "Анна") with { JoinedAt = Clock.Now.AddSeconds(-1) });
        var state = await store.JoinAsync(room, Member("b", "Борис"));

        Assert.Equal(["Анна", "Борис"], state.Members.Select(member => member.Name));
    }

    [Theory(DisplayName = "Store: a field is claimed by one connection and refused to the next")]
    [MemberData(nameof(Stores))]
    public async Task AcquireLease_IsExclusive(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.JoinAsync(room, Member("a", "Анна"));
        await store.JoinAsync(room, Member("b", "Борис"));

        var mine = await store.AcquireLeaseAsync(room, "/tasks/1/maxScore", "a");
        var theirs = await store.AcquireLeaseAsync(room, "/tasks/1/maxScore", "b");

        Assert.Equal("Анна", mine.Holder!.Name);
        Assert.Equal("Анна", theirs.Holder!.Name);
        Assert.NotNull(theirs.ExpiresAt);

        Assert.True(await store.MayWriteAsync(room, "/tasks/1/maxScore", "a"));
        Assert.False(await store.MayWriteAsync(room, "/tasks/1/maxScore", "b"));

        // A field nobody claimed is writable by anyone; the lease is per field, not per room.
        Assert.True(await store.MayWriteAsync(room, "/tasks/1/content", "b"));
    }

    [Theory(DisplayName = "Store: only the holder can release a lease")]
    [MemberData(nameof(Stores))]
    public async Task ReleaseLease_OnlyByHolder(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.JoinAsync(room, Member("a"));
        await store.JoinAsync(room, Member("b"));
        await store.AcquireLeaseAsync(room, "/title", "a");

        Assert.False(await store.ReleaseLeaseAsync(room, "/title", "b"));
        Assert.False(await store.MayWriteAsync(room, "/title", "b"));

        Assert.True(await store.ReleaseLeaseAsync(room, "/title", "a"));
        Assert.True(await store.MayWriteAsync(room, "/title", "b"));
    }

    // A tab that is killed rather than closed cleanly must not hold a field until the TTL; the
    // disconnect is the fast path, the TTL is the backstop.
    [Theory(DisplayName = "Store: leaving frees every field the connection held")]
    [MemberData(nameof(Stores))]
    public async Task Leave_FreesHeldFields(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.JoinAsync(room, Member("a"));
        await store.JoinAsync(room, Member("b"));
        await store.AcquireLeaseAsync(room, "/title", "a");
        await store.AcquireLeaseAsync(room, "/tasks/1/maxScore", "a");

        var freed = await store.LeaveAsync(room, "a");

        Assert.Equal(
            ["/tasks/1/maxScore", "/title"],
            freed.OrderBy(path => path, StringComparer.Ordinal)
        );
        Assert.True(await store.MayWriteAsync(room, "/title", "b"));
        Assert.Empty(await store.GetLeasesAsync(room));
        Assert.Single(await store.GetMembersAsync(room));
    }

    [Theory(DisplayName = "Store: a heartbeat from a member nobody knows reports that it must rejoin")]
    [MemberData(nameof(Stores))]
    public async Task Heartbeat_ReportsAnUnknownMember(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.JoinAsync(room, Member("a"));

        Assert.True(await store.HeartbeatAsync(room, "a"));
        Assert.False(await store.HeartbeatAsync(room, "ghost"));
    }

    [Theory(DisplayName = "Store: operations keep their order and their sequence numbers")]
    [MemberData(nameof(Stores))]
    public async Task AppendOps_IsSequential(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();

        Assert.Equal(2, await store.AppendOpsAsync(room, [Op("/a", 1), Op("/b", 2)]));
        Assert.Equal(3, await store.AppendOpsAsync(room, [Op("/c", 3)]));

        var log = await store.GetOpsAsync(room);

        Assert.Equal(3, log.Seq);
        Assert.Equal(["/a", "/b", "/c"], log.Ops.Select(op => op.Path));
        Assert.Equal(3, log.Ops[2].Value!.GetValue<int>());
    }

    // Compaction is what keeps a long session's draft bounded. Sequence numbers must survive it:
    // a client that applied up to 3 has to stay correct about what it has already seen.
    [Theory(DisplayName = "Store: compaction shortens the log without rewinding sequence numbers")]
    [MemberData(nameof(Stores))]
    public async Task CompactOps_KeepsSequenceMonotonic(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.AppendOpsAsync(room, [Op("/a", 1), Op("/b", 2), Op("/c", 3)]);

        Assert.True(await store.TryCompactOpsAsync(room, 3, [Op("", 0)]));

        var log = await store.GetOpsAsync(room);

        Assert.Equal(3, log.Seq);
        Assert.Single(log.Ops);

        Assert.Equal(4, await store.AppendOpsAsync(room, [Op("/d", 4)]));
    }

    [Theory(DisplayName = "Store: compaction is refused when the log moved under it")]
    [MemberData(nameof(Stores))]
    public async Task CompactOps_RefusesAStaleSwap(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.AppendOpsAsync(room, [Op("/a", 1), Op("/b", 2)]);

        Assert.False(await store.TryCompactOpsAsync(room, 1, [Op("", 0)]));
        Assert.Equal(2, (await store.GetOpsAsync(room)).Ops.Count);
    }

    [Theory(DisplayName = "Store: saving clears the draft and bumps the version")]
    [MemberData(nameof(Stores))]
    public async Task ClearDraft_BumpsTheVersion(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.AppendOpsAsync(room, [Op("/a", 1)]);

        Assert.Equal(0, await store.GetVersionAsync(room));
        Assert.Equal(1, await store.ClearDraftAsync(room));

        Assert.Empty((await store.GetOpsAsync(room)).Ops);
        Assert.Equal(1, await store.GetVersionAsync(room));
    }

    // Two clients both writing the starting content into one CRDT document is how a
    // collaborative editor ends up showing the whole task twice.
    [Theory(DisplayName = "Store: exactly one client is told to seed a scope")]
    [MemberData(nameof(Stores))]
    public async Task JoinScope_ElectsOneSeeder(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();

        var first = await store.JoinScopeAsync(room, "task-1", "a");
        var second = await store.JoinScopeAsync(room, "task-1", "b");

        Assert.True(first.MaySeed);
        Assert.Equal(1, first.MemberCount);
        Assert.False(second.MaySeed);
        Assert.Equal(2, second.MemberCount);
    }

    [Theory(DisplayName = "Store: an emptied scope is seeded again by whoever opens it next")]
    [MemberData(nameof(Stores))]
    public async Task LeaveScope_ClearsTheSeedClaim(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();

        await store.JoinScopeAsync(room, "task-1", "a");
        await store.LeaveScopeAsync(room, "task-1", "a");

        Assert.True((await store.JoinScopeAsync(room, "task-1", "b")).MaySeed);
    }

    [Theory(DisplayName = "Store: the scope a member is on is visible to the rest of the room")]
    [MemberData(nameof(Stores))]
    public async Task SetScope_ShowsUpInTheRoster(string kind)
    {
        var store = Create(kind);
        var room = NewRoom();
        await store.JoinAsync(room, Member("a"));

        await store.SetScopeAsync(room, "a", "task-5");

        Assert.Equal("task-5", (await store.GetMembersAsync(room)).Single().Scope);
    }

    private static bool IsRedisListening()
    {
        try
        {
            using var client = new TcpClient();

            return client.ConnectAsync(RedisHost, RedisPort).Wait(TimeSpan.FromMilliseconds(300))
                && client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
