using System.Text.Json;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Utils;
using StackExchange.Redis;

namespace Noo.Api.Core.System.Collaboration.Store;

/// <summary>
/// The fleet-wide room store. Every instance sees the same rooms, which is what makes a lease
/// taken on one pod visible to an editor connected to another.
/// </summary>
public sealed class RedisCollaborationStore : ICollaborationStore
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly CollaborationConfig _config;

    public RedisCollaborationStore(
        IConnectionMultiplexer redis,
        IOptions<CollaborationConfig> config
    )
    {
        _redis = redis;
        _config = config.Value;
    }

    private IDatabase Db => _redis.GetDatabase();

    private long RoomTtlMs => (long)_config.DraftTtl.TotalMilliseconds;

    private long LeaseTtlMs => (long)_config.LeaseTtl.TotalMilliseconds;

    public async Task<CollaborationRoomState> JoinAsync(
        CollaborationRoom room,
        CollaborationMember member,
        CancellationToken ct = default
    )
    {
        await PruneLapsedMembersAsync(room);

        var db = Db;

        await db.HashSetAsync(
            room.PresenceKey,
            member.ConnectionId,
            JsonSerializer.Serialize(member, _json)
        );
        await db.SortedSetAddAsync(room.PresenceSeenKey, member.ConnectionId, NowMs());
        await db.KeyExpireAsync(room.PresenceKey, _config.DraftTtl);
        await db.KeyExpireAsync(room.PresenceSeenKey, _config.DraftTtl);

        var members = await GetMembersAsync(room, ct);
        var leases = await GetLeasesAsync(room, ct);
        var log = await GetOpsAsync(room, ct);

        return new CollaborationRoomState
        {
            Seq = log.Seq,
            Version = await GetVersionAsync(room, ct),
            Members = members,
            Leases = leases,
            Ops = log.Ops,
        };
    }

    public async Task<IReadOnlyList<string>> LeaveAsync(
        CollaborationRoom room,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var freed = await Db.ScriptEvaluateAsync(
            CollaborationScripts.Leave,
            [
                room.PresenceKey,
                room.PresenceSeenKey,
                room.ConnectionLeasesKey(connectionId),
                room.LeasePathsKey,
            ],
            [connectionId, room.LeaseKeyPrefix]
        );

        return [.. ((RedisResult[])freed!).Select(path => (string)path!)];
    }

    public async Task<bool> HeartbeatAsync(
        CollaborationRoom room,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var alive = await Db.ScriptEvaluateAsync(
            CollaborationScripts.Heartbeat,
            [room.PresenceKey, room.PresenceSeenKey, room.ConnectionLeasesKey(connectionId)],
            [connectionId, NowMs(), LeaseTtlMs, room.LeaseKeyPrefix, RoomTtlMs]
        );

        return (long)alive == 1;
    }

    public async Task<IReadOnlyList<CollaborationMember>> GetMembersAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var entries = await Db.HashGetAllAsync(room.PresenceKey);

        return
        [
            .. entries
                .Select(entry =>
                    JsonSerializer.Deserialize<CollaborationMember>((string)entry.Value!, _json)
                )
                .Where(member => member is not null)
                .Select(member => member!)
                .OrderBy(member => member.JoinedAt),
        ];
    }

    public async Task SetScopeAsync(
        CollaborationRoom room,
        string connectionId,
        string? scope,
        CancellationToken ct = default
    )
    {
        var raw = await Db.HashGetAsync(room.PresenceKey, connectionId);

        if (raw.IsNullOrEmpty)
        {
            return;
        }

        var member = JsonSerializer.Deserialize<CollaborationMember>((string)raw!, _json)! with
        {
            Scope = scope,
        };

        await Db.HashSetAsync(
            room.PresenceKey,
            connectionId,
            JsonSerializer.Serialize(member, _json)
        );
    }

    public async Task<CollaborationLease> AcquireLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var result = (RedisResult[])(
            await Db.ScriptEvaluateAsync(
                CollaborationScripts.AcquireLease,
                [
                    room.LeaseKey(path),
                    room.ConnectionLeasesKey(connectionId),
                    room.LeasePathsKey,
                ],
                [connectionId, LeaseTtlMs, path, RoomTtlMs]
            )
        )!;

        var holderConnectionId = (string?)result[0];
        var ttlMs = (long)result[1];

        return await BuildLeaseAsync(room, path, holderConnectionId, ttlMs);
    }

    public async Task<bool> ReleaseLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var released = await Db.ScriptEvaluateAsync(
            CollaborationScripts.ReleaseLease,
            [room.LeaseKey(path), room.ConnectionLeasesKey(connectionId), room.LeasePathsKey],
            [connectionId, path]
        );

        return (long)released == 1;
    }

    public async Task<IReadOnlyList<CollaborationLease>> GetLeasesAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var db = Db;
        var paths = await db.SetMembersAsync(room.LeasePathsKey);
        var leases = new List<CollaborationLease>();

        foreach (var entry in paths)
        {
            var path = (string)entry!;
            var holder = await db.StringGetAsync(room.LeaseKey(path));
            var ttl = await db.KeyTimeToLiveAsync(room.LeaseKey(path));

            // The lease expired on its own; the index entry it left behind is dropped here
            // rather than by a sweep nobody would remember to schedule.
            if (holder.IsNullOrEmpty || ttl is null)
            {
                await db.SetRemoveAsync(room.LeasePathsKey, entry);
                continue;
            }

            leases.Add(
                await BuildLeaseAsync(room, path, (string)holder!, (long)ttl.Value.TotalMilliseconds)
            );
        }

        return leases;
    }

    public async Task<bool> MayWriteAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var holder = await Db.StringGetAsync(room.LeaseKey(path));

        return holder.IsNullOrEmpty || (string)holder! == connectionId;
    }

    public async Task<long> AppendOpsAsync(
        CollaborationRoom room,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        if (ops.Count == 0)
        {
            return (await GetOpsAsync(room, ct)).Seq;
        }

        RedisValue[] args =
        [
            RoomTtlMs,
            .. ops.Select(op => (RedisValue)JsonSerializer.Serialize(op, _json)),
        ];

        var seq = await Db.ScriptEvaluateAsync(
            CollaborationScripts.AppendOps,
            [room.OpsKey, room.SeqBaseKey],
            args
        );

        return (long)seq;
    }

    public async Task<CollaborationOpLog> GetOpsAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var db = Db;
        var raw = await db.ListRangeAsync(room.OpsKey);
        var seqBase = (long?)await db.StringGetAsync(room.SeqBaseKey) ?? 0;

        return new CollaborationOpLog(
            seqBase + raw.Length,
            [
                .. raw.Select(entry =>
                    JsonSerializer.Deserialize<CollaborationOp>((string)entry!, _json)!
                ),
            ]
        );
    }

    public async Task<bool> TryCompactOpsAsync(
        CollaborationRoom room,
        long expectedSeq,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        RedisValue[] args =
        [
            expectedSeq,
            RoomTtlMs,
            .. ops.Select(op => (RedisValue)JsonSerializer.Serialize(op, _json)),
        ];

        var compacted = await Db.ScriptEvaluateAsync(
            CollaborationScripts.CompactOps,
            [room.OpsKey, room.SeqBaseKey],
            args
        );

        return (long)compacted == 1;
    }

    public async Task<long> ClearDraftAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var version = await Db.ScriptEvaluateAsync(
            CollaborationScripts.ClearDraft,
            [room.OpsKey, room.SeqBaseKey, room.VersionKey]
        );

        await Db.KeyExpireAsync(room.VersionKey, _config.DraftTtl);

        return (long)version;
    }

    public async Task<long> GetVersionAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    ) => (long?)await Db.StringGetAsync(room.VersionKey) ?? 0;

    public async Task<CollaborationScopeState> JoinScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var result = (RedisResult[])(
            await Db.ScriptEvaluateAsync(
                CollaborationScripts.JoinScope,
                [room.ScopeMembersKey(scope), room.ScopeSeedKey(scope)],
                [connectionId, RoomTtlMs]
            )
        )!;

        return new CollaborationScopeState
        {
            MaySeed = (long)result[0] == 1,
            MemberCount = (int)(long)result[1],
        };
    }

    public Task LeaveScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId,
        CancellationToken ct = default
    ) =>
        Db.ScriptEvaluateAsync(
            CollaborationScripts.LeaveScope,
            [room.ScopeMembersKey(scope), room.ScopeSeedKey(scope)],
            [connectionId]
        );

    /// <summary>
    /// An instance that died holding connections leaves members no disconnect will ever clear.
    /// A join is the natural moment to notice, and costs one sorted-set range when nothing lapsed.
    /// </summary>
    private async Task PruneLapsedMembersAsync(CollaborationRoom room)
    {
        var cutoff = NowMs() - (long)_config.PresenceTtl.TotalMilliseconds;

        var lapsed = await Db.SortedSetRangeByScoreAsync(
            room.PresenceSeenKey,
            double.NegativeInfinity,
            cutoff
        );

        foreach (var connectionId in lapsed)
        {
            await LeaveAsync(room, connectionId!);
        }
    }

    private async Task<CollaborationLease> BuildLeaseAsync(
        CollaborationRoom room,
        string path,
        string? holderConnectionId,
        long ttlMs
    )
    {
        if (string.IsNullOrEmpty(holderConnectionId) || ttlMs <= 0)
        {
            return CollaborationLease.Released(path);
        }

        var raw = await Db.HashGetAsync(room.PresenceKey, holderConnectionId);

        return new CollaborationLease
        {
            Path = path,
            Holder = raw.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<CollaborationMember>((string)raw!, _json),
            ExpiresAt = Clock.Now.AddMilliseconds(ttlMs),
        };
    }

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
