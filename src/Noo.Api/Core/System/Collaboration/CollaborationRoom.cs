namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// One document being edited together: a work, later a course. The room type selects the
/// <see cref="ICollaborationRoomHandler"/> that knows how to authorise and save it, so nothing
/// below this line knows what a work is.
///
/// Deliberately says nothing about where the room's state is kept — that is each store's own
/// business, and <c>RedisRoomKeys</c> is where the Redis one answers it.
/// </summary>
public readonly record struct CollaborationRoom(string RoomType, Ulid RoomId)
{
    /// <summary>
    /// The SignalR group everything about this room is broadcast to.
    /// </summary>
    public string GroupName => $"collab:{RoomType}:{RoomId}";

    /// <summary>
    /// The group for one part of the document — a single task, say. CRDT frames are relayed here
    /// rather than to the whole room, so opening a work does not subscribe anyone to typing in
    /// the 299 tasks they are not looking at.
    /// </summary>
    public string ScopeGroupName(string scope) => $"{GroupName}:scope:{scope}";

    public override string ToString() => $"{RoomType}:{RoomId}";
}
