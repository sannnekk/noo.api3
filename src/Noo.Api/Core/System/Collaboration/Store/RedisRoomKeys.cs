namespace Noo.Api.Core.System.Collaboration.Store;

/// <summary>
/// Where one room's state lives in Redis.
///
/// This belongs to the store rather than to <see cref="CollaborationRoom"/>: a room is a document
/// several people are editing, not a set of Redis keys, and the in-memory store keys on the room
/// itself and uses none of this. Keeping the layout here is what lets a third store arrive
/// without the domain type growing a second vocabulary.
/// </summary>
internal readonly record struct RedisRoomKeys(CollaborationRoom Room)
{
    /// <summary>
    /// Every key of one room shares a hash tag, so all of them land on the same Redis slot. The
    /// scripts read and write several keys at once, which a cluster would otherwise refuse.
    /// </summary>
    private string Prefix => $"collab:{{{Room.RoomType}:{Room.RoomId}}}:";

    public string Presence => $"{Prefix}presence";

    public string PresenceSeen => $"{Prefix}presence:seen";

    public string Ops => $"{Prefix}ops";

    /// <summary>
    /// How many operations the log has shed to compaction or to a save. Sequence numbers are this
    /// plus the log's length, so neither sends them backwards under clients that already applied
    /// them.
    /// </summary>
    public string SeqBase => $"{Prefix}ops:base";

    public string Version => $"{Prefix}version";

    public string Lease(string path) => $"{Prefix}lease:{path}";

    public string LeasePrefix => $"{Prefix}lease:";

    /// <summary>
    /// Which paths currently hold a lease. Kept so listing them is a set read rather than a scan
    /// of the keyspace for a pattern — the leases themselves expire on their own, and entries
    /// left behind by an expiry are dropped the next time the set is read.
    /// </summary>
    public string LeasePaths => $"{Prefix}lease-paths";

    public string ConnectionLeases(string connectionId) => $"{Prefix}conn:{connectionId}:leases";

    public string ScopeMembers(string scope) => $"{Prefix}scope:{scope}:members";

    public string ScopeSeed(string scope) => $"{Prefix}scope:{scope}:seeded";
}
