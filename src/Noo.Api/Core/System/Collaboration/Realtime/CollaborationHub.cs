using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.Utils;
using Noo.Api.Users.Services;

namespace Noo.Api.Core.System.Collaboration.Realtime;

/// <summary>
/// One hub for every kind of collaboratively edited document. The room type on the wire selects
/// the handler that knows what the document is, so adding the course editor adds no hub, no
/// client contract and no endpoint — only a handler.
///
/// A connection belongs to exactly one room, which is what lets presence, leases and scopes all
/// key on the connection id alone.
/// </summary>
[Authorize(Policy = CollaborationPolicies.CanCollaborate)]
public class CollaborationHub : NooHub<ICollaborationHubClient>
{
    private const string _roomItemKey = "collab:room";
    private const string _scopesItemKey = "collab:scopes";

    private readonly ICollaborationStore _store;
    private readonly CollaborationRoomHandlerRegistry _handlers;
    private readonly IUserService _users;
    private readonly CollaborationConfig _config;

    public CollaborationHub(
        RealtimeMetrics metrics,
        RealtimeConnectionRegistry connections,
        ICollaborationStore store,
        CollaborationRoomHandlerRegistry handlers,
        IUserService users,
        IOptions<CollaborationConfig> config
    )
        : base(metrics, connections)
    {
        _store = store;
        _handlers = handlers;
        _users = users;
        _config = config.Value;
    }

    public async Task<CollaborationRoomState> JoinAsync(string roomType, Ulid roomId)
    {
        var handler = _handlers.Resolve(roomType);

        if (!await handler.CanEditAsync(roomId, Context.ConnectionAborted))
        {
            throw new ForbiddenException("You may not edit this document.");
        }

        var room = new CollaborationRoom(handler.RoomType, roomId);

        // Leaving whatever was open first: a client that navigates between two works reuses its
        // connection, and without this it would stay in the roster of the one it left.
        await LeaveAsync();

        if ((await _store.GetMembersAsync(room)).Count >= _config.MaxMembersPerRoom)
        {
            throw new ConflictException(
                $"This document already has {_config.MaxMembersPerRoom} editors."
            );
        }

        var user = await _users.GetUserByIdAsync(CallerId);

        var member = new CollaborationMember
        {
            ConnectionId = Context.ConnectionId,
            UserId = CallerId,
            Name = user?.Name ?? "—",
            Role = CallerRole ?? UserRoles.Teacher,
            JoinedAt = Clock.Now,
        };

        Context.Items[_roomItemKey] = room;

        var state = await _store.JoinAsync(room, member);

        await Groups.AddToGroupAsync(Context.ConnectionId, room.GroupName);
        await Clients.Group(room.GroupName).PresenceChangedAsync([.. state.Members]);

        return state;
    }

    public async Task LeaveAsync()
    {
        if (Context.Items.Remove(_roomItemKey, out var raw) && raw is CollaborationRoom room)
        {
            await ReleaseMembershipAsync(room);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.GroupName);
        }
    }

    /// <summary>
    /// Keeps the member and its leases alive. Returns false when the room forgot this connection
    /// — a pruned member, or an instance that restarted — so the client rejoins instead of
    /// heartbeating into nothing while believing it still holds a field.
    /// </summary>
    public Task<bool> HeartbeatAsync() =>
        CurrentRoom() is { } room ? _store.HeartbeatAsync(room, Context.ConnectionId) : Task.FromResult(false);

    public async Task SetScopeAsync(string? scope)
    {
        var room = RequireRoom();

        await _store.SetScopeAsync(room, Context.ConnectionId, scope);
        await BroadcastPresenceAsync(room);
    }

    public async Task<CollaborationLease> AcquireLeaseAsync(string path)
    {
        var room = RequireRoom();

        RequireAllowedPath(room, path);

        var lease = await _store.AcquireLeaseAsync(room, path, Context.ConnectionId);

        // Only the winner's claim is news. Losing a race changes nothing anyone else can see, and
        // announcing it would repaint the field for every editor in the room.
        if (lease.Holder?.ConnectionId == Context.ConnectionId)
        {
            await Clients.OthersInGroup(room.GroupName).LeaseChangedAsync(lease);
        }

        return lease;
    }

    public async Task ReleaseLeaseAsync(string path)
    {
        var room = RequireRoom();

        if (await _store.ReleaseLeaseAsync(room, path, Context.ConnectionId))
        {
            await Clients
                .OthersInGroup(room.GroupName)
                .LeaseChangedAsync(CollaborationLease.Released(path));
        }
    }

    /// <summary>
    /// Commits edits to the shared draft. The lease check is repeated here rather than trusted
    /// from the client: an input that disables itself is being polite, not authoritative.
    /// </summary>
    public async Task<long> PushOpsAsync(CollaborationOp[] ops)
    {
        var room = RequireRoom();

        if (ops.Length == 0)
        {
            return (await _store.GetOpsAsync(room)).Seq;
        }

        foreach (var op in ops)
        {
            RequireAllowedPath(room, op.Path);

            if (JsonSerializer.Serialize(op).Length > _config.MaxOpBytes)
            {
                throw new BadRequestException($"Operation on '{op.Path}' is too large.");
            }

            if (!await _store.MayWriteAsync(room, op.Path, Context.ConnectionId))
            {
                throw new ConflictException($"'{op.Path}' is being edited by someone else.");
            }
        }

        var seq = await _store.AppendOpsAsync(room, ops);

        await Clients
            .OthersInGroup(room.GroupName)
            .OpsAppliedAsync(seq - ops.Length + 1, ops);

        await CompactIfLongAsync(room);

        return seq;
    }

    /// <summary>
    /// Opens the CRDT channel for one part of the document — a single task, for a work. Scoped
    /// rather than room-wide so opening a work does not subscribe anyone to typing in the 299
    /// tasks they are not looking at, which is what keeps a 300-task work as cheap as a one-task
    /// one.
    /// </summary>
    public async Task<CollaborationScopeState> JoinScopeAsync(string scope)
    {
        var room = RequireRoom();

        await Groups.AddToGroupAsync(Context.ConnectionId, room.ScopeGroupName(scope));
        JoinedScopes().Add(scope);

        return await _store.JoinScopeAsync(room, scope, Context.ConnectionId);
    }

    public async Task LeaveScopeAsync(string scope)
    {
        var room = RequireRoom();

        JoinedScopes().Remove(scope);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.ScopeGroupName(scope));
        await _store.LeaveScopeAsync(room, scope, Context.ConnectionId);
    }

    /// <summary>
    /// Relays one CRDT frame to the others in its scope. The payload is never decoded: it is a
    /// y-protocols sync or awareness message, and keeping it opaque is exactly what lets
    /// multi-caret rich text work with no CRDT implementation on this side and no second server.
    ///
    /// Sent to the others, never back to the sender — a client has already applied its own
    /// update locally, and echoing it would cost a round trip per keystroke.
    /// </summary>
    public Task PushYjsAsync(CollaborationFrame frame)
    {
        var room = RequireRoom();

        if (!JoinedScopes().Contains(frame.Scope))
        {
            throw new BadRequestException($"Not in scope '{frame.Scope}'.");
        }

        if (frame.Payload.Length > _config.MaxYjsFrameBytes)
        {
            throw new BadRequestException("Frame is too large; sync it over HTTP instead.");
        }

        return Clients.OthersInGroup(room.ScopeGroupName(frame.Scope)).YjsFrameAsync(frame);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (CurrentRoom() is { } room)
        {
            await ReleaseMembershipAsync(room);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// The scopes this connection opened, so a disconnect can clear the seed claim it holds —
    /// without that, an emptied scope would never be seeded again and the next editor to open
    /// the task would find it blank.
    /// </summary>
    private HashSet<string> JoinedScopes()
    {
        if (Context.Items.TryGetValue(_scopesItemKey, out var raw) && raw is HashSet<string> scopes)
        {
            return scopes;
        }

        var created = new HashSet<string>(StringComparer.Ordinal);
        Context.Items[_scopesItemKey] = created;

        return created;
    }

    private CollaborationRoom? CurrentRoom() =>
        Context.Items.TryGetValue(_roomItemKey, out var raw) && raw is CollaborationRoom room
            ? room
            : null;

    private CollaborationRoom RequireRoom() =>
        CurrentRoom() ?? throw new BadRequestException("Join a room before editing it.");

    private void RequireAllowedPath(CollaborationRoom room, string path)
    {
        if (!_handlers.Resolve(room.RoomType).IsPathAllowed(path))
        {
            throw new BadRequestException($"'{path}' is not editable.");
        }
    }

    /// <summary>
    /// Drops the connection from the roster and frees every field it held, telling the room about
    /// each one — a killed tab must not hold a field until its lease lapses.
    /// </summary>
    private async Task ReleaseMembershipAsync(CollaborationRoom room)
    {
        foreach (var scope in JoinedScopes().ToArray())
        {
            await _store.LeaveScopeAsync(room, scope, Context.ConnectionId);
        }

        JoinedScopes().Clear();

        var freed = await _store.LeaveAsync(room, Context.ConnectionId);

        foreach (var path in freed)
        {
            await Clients
                .OthersInGroup(room.GroupName)
                .LeaseChangedAsync(CollaborationLease.Released(path));
        }

        await BroadcastPresenceAsync(room);
    }

    private async Task BroadcastPresenceAsync(CollaborationRoom room) =>
        await Clients
            .Group(room.GroupName)
            .PresenceChangedAsync([.. await _store.GetMembersAsync(room)]);

    /// <summary>
    /// Collapses a long session's draft into a single whole-document operation, so an afternoon
    /// of editing does not grow the room without bound. Clients cannot catch up incrementally
    /// across that, so they are told to refetch.
    /// </summary>
    private async Task CompactIfLongAsync(CollaborationRoom room)
    {
        var log = await _store.GetOpsAsync(room);

        if (log.Ops.Count <= _config.MaxOps)
        {
            return;
        }

        var compacted = await _handlers
            .Resolve(room.RoomType)
            .CompactAsync(room.RoomId, log.Ops, Context.ConnectionAborted);

        if (await _store.TryCompactOpsAsync(room, log.Seq, compacted))
        {
            await Clients.Group(room.GroupName).ResyncRequiredAsync("compacted");
        }
    }
}
