using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Utils;

namespace Noo.Api.Core.System.Collaboration.Store;

/// <summary>
/// The fallback when there is no Redis: correct for one instance, and only one. A lease taken
/// here is invisible to a second pod, so running the fleet on it would let two teachers hold the
/// same field. It exists so local development and the test suite need no Redis, and so a cache
/// outage degrades a room to single-instance rather than breaking the editor.
/// </summary>
public sealed class InMemoryCollaborationStore : ICollaborationStore
{
    private readonly ConcurrentDictionary<string, RoomState> _rooms = new(StringComparer.Ordinal);
    private readonly CollaborationConfig _config;

    public InMemoryCollaborationStore(IOptions<CollaborationConfig> config)
    {
        _config = config.Value;
    }

    public Task<CollaborationRoomState> JoinAsync(
        CollaborationRoom room,
        CollaborationMember member,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            PruneLapsed(state);

            state.Members[member.ConnectionId] = member;
            state.LastSeen[member.ConnectionId] = Clock.Now;

            return Task.FromResult(
                new CollaborationRoomState
                {
                    Seq = state.SeqBase + state.Ops.Count,
                    Version = state.Version,
                    Members = OrderedMembers(state),
                    Leases = LiveLeases(state),
                    Ops = [.. state.Ops],
                }
            );
        }
    }

    public Task<IReadOnlyList<string>> LeaveAsync(
        CollaborationRoom room,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            return Task.FromResult(Remove(state, connectionId));
        }
    }

    public Task<bool> HeartbeatAsync(
        CollaborationRoom room,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            if (!state.Members.ContainsKey(connectionId))
            {
                return Task.FromResult(false);
            }

            state.LastSeen[connectionId] = Clock.Now;

            foreach (var (path, lease) in state.Leases)
            {
                if (lease.ConnectionId == connectionId)
                {
                    state.Leases[path] = lease with { ExpiresAt = Clock.Now + _config.LeaseTtl };
                }
            }

            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<CollaborationMember>> GetMembersAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            return Task.FromResult(OrderedMembers(state));
        }
    }

    public Task SetScopeAsync(
        CollaborationRoom room,
        string connectionId,
        string? scope,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            if (state.Members.TryGetValue(connectionId, out var member))
            {
                state.Members[connectionId] = member with { Scope = scope };
            }
        }

        return Task.CompletedTask;
    }

    public Task<CollaborationLease> AcquireLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            var standing = LiveLease(state, path);

            if (standing is not null && standing.ConnectionId != connectionId)
            {
                return Task.FromResult(ToDto(state, standing, path));
            }

            var held = new HeldLease(connectionId, Clock.Now + _config.LeaseTtl);
            state.Leases[path] = held;

            return Task.FromResult(ToDto(state, held, path));
        }
    }

    public Task<bool> ReleaseLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            if (LiveLease(state, path) is { } lease && lease.ConnectionId == connectionId)
            {
                state.Leases.Remove(path);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<CollaborationLease>> GetLeasesAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            return Task.FromResult(LiveLeases(state));
        }
    }

    public Task<bool> MayWriteAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            var lease = LiveLease(state, path);

            return Task.FromResult(lease is null || lease.ConnectionId == connectionId);
        }
    }

    public Task<long> AppendOpsAsync(
        CollaborationRoom room,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            state.Ops.AddRange(ops);

            return Task.FromResult(state.SeqBase + state.Ops.Count);
        }
    }

    public Task<CollaborationOpLog> GetOpsAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            return Task.FromResult(
                new CollaborationOpLog(state.SeqBase + state.Ops.Count, [.. state.Ops])
            );
        }
    }

    public Task<bool> TryCompactOpsAsync(
        CollaborationRoom room,
        long expectedSeq,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            if (state.SeqBase + state.Ops.Count != expectedSeq)
            {
                return Task.FromResult(false);
            }

            state.SeqBase += state.Ops.Count - ops.Count;
            state.Ops.Clear();
            state.Ops.AddRange(ops);

            return Task.FromResult(true);
        }
    }

    public Task<long> ClearDraftAsync(CollaborationRoom room, CancellationToken ct = default)
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            state.Ops.Clear();
            state.SeqBase = 0;
            state.Version += 1;

            return Task.FromResult(state.Version);
        }
    }

    public Task<long> GetVersionAsync(CollaborationRoom room, CancellationToken ct = default)
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            return Task.FromResult(state.Version);
        }
    }

    public Task<CollaborationScopeState> JoinScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            if (!state.Scopes.TryGetValue(scope, out var members))
            {
                members = state.Scopes[scope] = new HashSet<string>(StringComparer.Ordinal);
            }

            members.Add(connectionId);

            var maySeed = state.Seeded.Add(scope) && members.Count == 1;

            return Task.FromResult(
                new CollaborationScopeState { MaySeed = maySeed, MemberCount = members.Count }
            );
        }
    }

    public Task LeaveScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId,
        CancellationToken ct = default
    )
    {
        var state = StateOf(room);

        lock (state.Gate)
        {
            if (state.Scopes.TryGetValue(scope, out var members) && members.Remove(connectionId)
                && members.Count == 0)
            {
                state.Scopes.Remove(scope);
                state.Seeded.Remove(scope);
            }
        }

        return Task.CompletedTask;
    }

    private RoomState StateOf(CollaborationRoom room) =>
        _rooms.GetOrAdd(room.ToString(), _ => new RoomState());

    private IReadOnlyList<string> Remove(RoomState state, string connectionId)
    {
        state.Members.Remove(connectionId);
        state.LastSeen.Remove(connectionId);

        var freed = state
            .Leases.Where(entry => entry.Value.ConnectionId == connectionId)
            .Select(entry => entry.Key)
            .ToArray();

        foreach (var path in freed)
        {
            state.Leases.Remove(path);
        }

        foreach (var (scope, members) in state.Scopes.ToArray())
        {
            if (members.Remove(connectionId) && members.Count == 0)
            {
                state.Scopes.Remove(scope);
                state.Seeded.Remove(scope);
            }
        }

        return freed;
    }

    private void PruneLapsed(RoomState state)
    {
        var cutoff = Clock.Now - _config.PresenceTtl;

        foreach (var connectionId in state.LastSeen.Where(e => e.Value < cutoff).Select(e => e.Key).ToArray())
        {
            Remove(state, connectionId);
        }
    }

    private static IReadOnlyList<CollaborationMember> OrderedMembers(RoomState state) =>
        [.. state.Members.Values.OrderBy(member => member.JoinedAt)];

    private static HeldLease? LiveLease(RoomState state, string path) =>
        state.Leases.TryGetValue(path, out var lease) && lease.ExpiresAt > Clock.Now ? lease : null;

    private IReadOnlyList<CollaborationLease> LiveLeases(RoomState state)
    {
        foreach (var path in state.Leases.Where(e => e.Value.ExpiresAt <= Clock.Now).Select(e => e.Key).ToArray())
        {
            state.Leases.Remove(path);
        }

        return [.. state.Leases.Select(entry => ToDto(state, entry.Value, entry.Key))];
    }

    private static CollaborationLease ToDto(RoomState state, HeldLease lease, string path) =>
        new()
        {
            Path = path,
            Holder = state.Members.GetValueOrDefault(lease.ConnectionId),
            ExpiresAt = lease.ExpiresAt,
        };

    private sealed record HeldLease(string ConnectionId, DateTime ExpiresAt);

    private sealed class RoomState
    {
        public Lock Gate { get; } = new();
        public Dictionary<string, CollaborationMember> Members { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, DateTime> LastSeen { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, HeldLease> Leases { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, HashSet<string>> Scopes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Seeded { get; } = new(StringComparer.Ordinal);
        public List<CollaborationOp> Ops { get; } = [];
        public long SeqBase { get; set; }
        public long Version { get; set; }
    }
}
