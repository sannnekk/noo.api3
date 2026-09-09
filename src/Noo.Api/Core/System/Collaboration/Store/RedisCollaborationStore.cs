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

    public async Task<CollaborationRoomState?> TryJoinAsync(
        CollaborationRoom room,
        CollaborationMember member
    )
    {
        var keys = new RedisRoomKeys(room);

        // Before the cap is applied, so a room full of members an instance died holding is not a
        // room nobody can get back into.
        await PruneLapsedMembersAsync(room);

        var joined = await Db.ScriptEvaluateAsync(
            CollaborationScripts.Join,
            [keys.Presence, keys.PresenceSeen],
            [
                member.ConnectionId,
                JsonSerializer.Serialize(member, _json),
                NowMs(),
                RoomTtlMs,
                _config.MaxMembersPerRoom,
            ]
        );

        if ((long)joined == 0)
        {
            return null;
        }

        var members = await GetMembersAsync(room);
        var leases = await GetLeasesAsync(room);
        var log = await GetOpsAsync(room);

        return new CollaborationRoomState
        {
            Seq = log.Seq,
            Members = members,
            Leases = leases,
            Ops = log.Ops,
        };
    }

    public async Task<IReadOnlyList<string>> LeaveAsync(
        CollaborationRoom room,
        string connectionId
    )
    {
        var keys = new RedisRoomKeys(room);

        var freed = await Db.ScriptEvaluateAsync(
            CollaborationScripts.Leave,
            [
                keys.Presence,
                keys.PresenceSeen,
                keys.ConnectionLeases(connectionId),
                keys.LeasePaths,
            ],
            [connectionId, keys.LeasePrefix]
        );

        return [.. ((RedisResult[])freed!).Select(path => (string)path!)];
    }

    public async Task<bool> HeartbeatAsync(
        CollaborationRoom room,
        string connectionId
    )
    {
        var keys = new RedisRoomKeys(room);

        var alive = await Db.ScriptEvaluateAsync(
            CollaborationScripts.Heartbeat,
            [keys.Presence, keys.PresenceSeen, keys.ConnectionLeases(connectionId)],
            [connectionId, NowMs(), LeaseTtlMs, keys.LeasePrefix, RoomTtlMs]
        );

        return (long)alive == 1;
    }

    public async Task<IReadOnlyList<CollaborationMember>> GetMembersAsync(
        CollaborationRoom room
    )
    {
        var keys = new RedisRoomKeys(room);

        var entries = await Db.HashGetAllAsync(keys.Presence);

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
        string? scope
    )
    {
        var keys = new RedisRoomKeys(room);

        var raw = await Db.HashGetAsync(keys.Presence, connectionId);

        if (raw.IsNullOrEmpty)
        {
            return;
        }

        var member = JsonSerializer.Deserialize<CollaborationMember>((string)raw!, _json)! with
        {
            Scope = scope,
        };

        await Db.HashSetAsync(
            keys.Presence,
            connectionId,
            JsonSerializer.Serialize(member, _json)
        );
    }

    public async Task<CollaborationLease> AcquireLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId
    )
    {
        var keys = new RedisRoomKeys(room);

        var result = (RedisResult[])(
            await Db.ScriptEvaluateAsync(
                CollaborationScripts.AcquireLease,
                [
                    keys.Lease(path),
                    keys.ConnectionLeases(connectionId),
                    keys.LeasePaths,
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
        string connectionId
    )
    {
        var keys = new RedisRoomKeys(room);

        var released = await Db.ScriptEvaluateAsync(
            CollaborationScripts.ReleaseLease,
            [keys.Lease(path), keys.ConnectionLeases(connectionId), keys.LeasePaths],
            [connectionId, path]
        );

        return (long)released == 1;
    }

    public async Task<IReadOnlyList<CollaborationLease>> GetLeasesAsync(
        CollaborationRoom room
    )
    {
        var keys = new RedisRoomKeys(room);

        // Two round trips whatever the room holds: the leases, then the roster to name their
        // holders. A GET and a PTTL per path would grow with the number of fields being edited,
        // and this runs on every join.
        var rows = (RedisResult[])(
            await Db.ScriptEvaluateAsync(
                CollaborationScripts.GetLeases,
                [keys.LeasePaths],
                [keys.LeasePrefix]
            )
        )!;

        if (rows.Length == 0)
        {
            return [];
        }

        var holders = await MembersByConnectionAsync(room);
        var leases = new List<CollaborationLease>(rows.Length / 3);

        for (var index = 0; index + 2 < rows.Length; index += 3)
        {
            var ttlMs = long.Parse((string)rows[index + 2]!);

            if (ttlMs <= 0)
            {
                continue;
            }

            leases.Add(
                new CollaborationLease
                {
                    Path = (string)rows[index]!,
                    Holder = holders.GetValueOrDefault((string)rows[index + 1]!),
                    ExpiresAt = Clock.Now.AddMilliseconds(ttlMs),
                }
            );
        }

        return leases;
    }

    private async Task<Dictionary<string, CollaborationMember>> MembersByConnectionAsync(
        CollaborationRoom room
    ) =>
        (await GetMembersAsync(room)).ToDictionary(
            member => member.ConnectionId,
            StringComparer.Ordinal
        );

    public async Task<bool> MayWriteAsync(
        CollaborationRoom room,
        string path,
        string connectionId
    )
    {
        var keys = new RedisRoomKeys(room);

        var allowed = await Db.ScriptEvaluateAsync(
            CollaborationScripts.MayWrite,
            [keys.LeasePaths],
            [path, connectionId, keys.LeasePrefix]
        );

        return (long)allowed == 1;
    }

    public async Task<long> AppendOpsAsync(
        CollaborationRoom room,
        IReadOnlyList<CollaborationOp> ops
    )
    {
        var keys = new RedisRoomKeys(room);

        if (ops.Count == 0)
        {
            return (await GetOpsAsync(room)).Seq;
        }

        RedisValue[] args =
        [
            RoomTtlMs,
            .. ops.Select(op => (RedisValue)JsonSerializer.Serialize(op, _json)),
        ];

        var seq = await Db.ScriptEvaluateAsync(
            CollaborationScripts.AppendOps,
            [keys.Ops, keys.SeqBase],
            args
        );

        return (long)seq;
    }

    public async Task<CollaborationOpLog> GetOpsAsync(
        CollaborationRoom room
    )
    {
        var keys = new RedisRoomKeys(room);

        var db = Db;
        var raw = await db.ListRangeAsync(keys.Ops);
        var seqBase = (long?)await db.StringGetAsync(keys.SeqBase) ?? 0;

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
        IReadOnlyList<CollaborationOp> ops
    )
    {
        var keys = new RedisRoomKeys(room);

        RedisValue[] args =
        [
            expectedSeq,
            RoomTtlMs,
            .. ops.Select(op => (RedisValue)JsonSerializer.Serialize(op, _json)),
        ];

        var compacted = await Db.ScriptEvaluateAsync(
            CollaborationScripts.CompactOps,
            [keys.Ops, keys.SeqBase],
            args
        );

        return (long)compacted == 1;
    }

    public async Task<bool> TryTrimDraftAsync(
        CollaborationRoom room,
        long throughSeq
    )
    {
        var keys = new RedisRoomKeys(room);

        var trimmed = await Db.ScriptEvaluateAsync(
            CollaborationScripts.TrimDraft,
            [keys.Ops, keys.SeqBase],
            [throughSeq, RoomTtlMs]
        );

        return (long)trimmed == 1;
    }

    public Task ClearDraftAsync(CollaborationRoom room)
    {
        var keys = new RedisRoomKeys(room);

        return Db.ScriptEvaluateAsync(
            CollaborationScripts.ClearDraft,
            [keys.Ops, keys.SeqBase],
            [RoomTtlMs]
        );
    }


    public async Task<CollaborationScopeState> JoinScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId
    )
    {
        var keys = new RedisRoomKeys(room);

        var result = (RedisResult[])(
            await Db.ScriptEvaluateAsync(
                CollaborationScripts.JoinScope,
                [keys.ScopeMembers(scope), keys.ScopeSeed(scope)],
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
        string connectionId
    ) =>
        Db.ScriptEvaluateAsync(
            CollaborationScripts.LeaveScope,
            [Keys(room).ScopeMembers(scope), Keys(room).ScopeSeed(scope)],
            [connectionId]
        );

    /// <summary>
    /// An instance that died holding connections leaves members no disconnect will ever clear.
    /// A join is the natural moment to notice, and costs one sorted-set range when nothing lapsed.
    /// </summary>
    private async Task PruneLapsedMembersAsync(CollaborationRoom room)
    {
        var keys = new RedisRoomKeys(room);

        var cutoff = NowMs() - (long)_config.PresenceTtl.TotalMilliseconds;

        var lapsed = await Db.SortedSetRangeByScoreAsync(
            keys.PresenceSeen,
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
        var keys = new RedisRoomKeys(room);

        if (string.IsNullOrEmpty(holderConnectionId) || ttlMs <= 0)
        {
            return CollaborationLease.Released(path);
        }

        var raw = await Db.HashGetAsync(keys.Presence, holderConnectionId);

        return new CollaborationLease
        {
            Path = path,
            Holder = raw.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<CollaborationMember>((string)raw!, _json),
            ExpiresAt = Clock.Now.AddMilliseconds(ttlMs),
        };
    }

    private static RedisRoomKeys Keys(CollaborationRoom room) => new(room);

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
