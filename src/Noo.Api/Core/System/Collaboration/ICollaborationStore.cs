namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// Where a room's shared state lives while it is being edited. Everything here is deliberately
/// short-lived and reconstructible: the entity in MySQL plus this store is the whole truth, and
/// losing the store costs unsaved edits, never saved ones.
/// </summary>
public interface ICollaborationStore
{
    /// <summary>
    /// Adds a member and returns the room as it now stands, or null when the room is already
    /// full. The cap is enforced here rather than counted first and joined after, because two
    /// connections arriving together would both read a roster with room in it.
    ///
    /// Also prunes members whose heartbeat lapsed — an instance that died holding connections
    /// leaves entries no disconnect will ever clear, and a join is the natural moment to notice.
    /// </summary>
    public Task<CollaborationRoomState?> TryJoinAsync(
        CollaborationRoom room,
        CollaborationMember member,
        CancellationToken ct = default
    );

    /// <summary>Removes a member and every lease it held. Returns the paths that were freed.</summary>
    public Task<IReadOnlyList<string>> LeaveAsync(
        CollaborationRoom room,
        string connectionId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Keeps a member and its leases alive. Returns false when the member was already pruned, so
    /// the caller can rejoin rather than heartbeat into nothing.
    /// </summary>
    public Task<bool> HeartbeatAsync(
        CollaborationRoom room,
        string connectionId,
        CancellationToken ct = default
    );

    public Task<IReadOnlyList<CollaborationMember>> GetMembersAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    );

    public Task SetScopeAsync(
        CollaborationRoom room,
        string connectionId,
        string? scope,
        CancellationToken ct = default
    );

    /// <summary>
    /// Claims a field for one connection, or returns the lease that already stands. Never steals:
    /// the only ways a lease changes hands are release, disconnect and expiry.
    /// </summary>
    public Task<CollaborationLease> AcquireLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    );

    /// <summary>Releases a lease, but only if this connection still holds it.</summary>
    public Task<bool> ReleaseLeaseAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    );

    public Task<IReadOnlyList<CollaborationLease>> GetLeasesAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    );

    /// <summary>
    /// Whether this connection may write to a path: no lease held by anyone else covers it or
    /// anything it contains. Enforced on the server because a client that disables an input is
    /// being polite, not authoritative — and covering the whole subtree because removing a task
    /// is a write to every field somebody might be holding inside it.
    /// </summary>
    public Task<bool> MayWriteAsync(
        CollaborationRoom room,
        string path,
        string connectionId,
        CancellationToken ct = default
    );

    /// <summary>Appends to the draft. Returns the sequence number of the last operation added.</summary>
    public Task<long> AppendOpsAsync(
        CollaborationRoom room,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    );

    public Task<CollaborationOpLog> GetOpsAsync(
        CollaborationRoom room,
        CancellationToken ct = default
    );

    /// <summary>
    /// Swaps the whole log for a shorter one, only if it is still the length the caller read.
    /// Used to collapse a long session into a single whole-document operation; sequence numbers
    /// carry on from where they were rather than restarting.
    /// </summary>
    public Task<bool> TryCompactOpsAsync(
        CollaborationRoom room,
        long expectedSeq,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    );

    /// <summary>
    /// Drops the operations a save actually applied — those up to <paramref name="throughSeq"/> —
    /// and leaves anything appended while that save was running. Returns the new version, or null
    /// when the log no longer starts where the caller thinks it does, which means a compaction or
    /// a discard got there first.
    ///
    /// Clearing the whole draft instead would throw away the edits someone made during the
    /// seconds the save spent in MySQL, and they would be gone with no sign that they existed.
    /// </summary>
    public Task<long?> TryTrimDraftAsync(
        CollaborationRoom room,
        long throughSeq,
        CancellationToken ct = default
    );

    /// <summary>
    /// Throws the whole draft away unconditionally and bumps the version. This is the discard
    /// path; a save uses <see cref="TryTrimDraftAsync"/> so it drops only what it saved.
    /// </summary>
    public Task<long> ClearDraftAsync(CollaborationRoom room, CancellationToken ct = default);

    public Task<long> GetVersionAsync(CollaborationRoom room, CancellationToken ct = default);

    /// <summary>
    /// Adds a connection to a CRDT scope and says whether it is the one that must seed the
    /// document. Exactly one member of an empty scope is told yes.
    /// </summary>
    public Task<CollaborationScopeState> JoinScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Leaves a CRDT scope, clearing the seed claim when the last member goes so the next arrival
    /// starts the document again from what was saved.
    /// </summary>
    public Task LeaveScopeAsync(
        CollaborationRoom room,
        string scope,
        string connectionId,
        CancellationToken ct = default
    );
}
