namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// One document being edited together: a work, later a course. The room type selects the
/// <see cref="ICollaborationRoomHandler"/> that knows how to authorise and save it, so nothing
/// below this line knows what a work is.
/// </summary>
public readonly record struct CollaborationRoom(string RoomType, Ulid RoomId)
{
    /// <summary>
    /// Every key of one room shares a hash tag, so all of them land on the same Redis slot. The
    /// scripts below read and write several keys at once, which a cluster would otherwise refuse.
    /// </summary>
    public string KeyPrefix => $"collab:{{{RoomType}:{RoomId}}}:";

    /// <summary>
    /// The SignalR group everything about this room is broadcast to. Not the Redis prefix: group
    /// names go over the backplane and gain nothing from the hash tag.
    /// </summary>
    public string GroupName => $"collab:{RoomType}:{RoomId}";

    /// <summary>
    /// The group for one part of the document — a single task, say. CRDT frames are relayed here
    /// rather than to the whole room, so opening a work does not subscribe anyone to typing in
    /// the 299 tasks they are not looking at.
    /// </summary>
    public string ScopeGroupName(string scope) => $"{GroupName}:scope:{scope}";

    public string PresenceKey => $"{KeyPrefix}presence";

    public string PresenceSeenKey => $"{KeyPrefix}presence:seen";

    public string OpsKey => $"{KeyPrefix}ops";

    /// <summary>
    /// How many operations the log has shed to compaction. Sequence numbers are this plus the
    /// log's length, so compacting does not send them backwards under clients that already
    /// applied them.
    /// </summary>
    public string SeqBaseKey => $"{KeyPrefix}ops:base";

    public string VersionKey => $"{KeyPrefix}version";

    public string LeaseKey(string path) => $"{KeyPrefix}lease:{path}";

    public string LeaseKeyPrefix => $"{KeyPrefix}lease:";

    /// <summary>
    /// Which paths currently hold a lease. Kept so listing them is a set read rather than a scan
    /// of the keyspace for a pattern — the leases themselves expire on their own, and entries
    /// left behind by an expiry are dropped the next time the set is read.
    /// </summary>
    public string LeasePathsKey => $"{KeyPrefix}lease-paths";

    public string ConnectionLeasesKey(string connectionId) =>
        $"{KeyPrefix}conn:{connectionId}:leases";

    public string ScopeMembersKey(string scope) => $"{KeyPrefix}scope:{scope}:members";

    public string ScopeSeedKey(string scope) => $"{KeyPrefix}scope:{scope}:seeded";

    public override string ToString() => $"{RoomType}:{RoomId}";
}
