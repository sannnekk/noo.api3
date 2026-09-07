namespace Noo.Api.Core.System.Collaboration.Realtime;

/// <summary>
/// What the server pushes to everyone in a room. Deliberately small: the entity itself is never
/// pushed, only what changed about the shared draft, so a client always renders the document it
/// fetched over HTTP plus the operations it has applied.
/// </summary>
public interface ICollaborationHubClient
{
    /// <summary>
    /// The whole roster rather than a join/leave delta. A room holds a handful of people, and a
    /// full list is idempotent — a client that missed one message is not left believing someone
    /// is still in the document.
    /// </summary>
    public Task PresenceChangedAsync(CollaborationMember[] members);

    public Task LeaseChangedAsync(CollaborationLease lease);

    /// <summary>
    /// Operations someone else committed, numbered from <paramref name="fromSeq"/>. A client that
    /// sees a gap knows it missed something and refetches rather than applying out of order.
    /// </summary>
    public Task OpsAppliedAsync(long fromSeq, CollaborationOp[] ops);

    /// <summary>
    /// Carries no entity: media URLs pushed over a hub would ship unsigned, and everyone has to
    /// re-read anyway to pick up what the server settled (task numbering, the total score).
    /// </summary>
    public Task DocumentSavedAsync(CollaborationSaved saved);

    /// <summary>
    /// Sent when a client's own view of the draft cannot be trusted to catch up incrementally —
    /// after compaction, or after a gap. The client refetches the room state.
    /// </summary>
    public Task ResyncRequiredAsync(string reason);

    /// <summary>Relays an opaque CRDT frame from another editor in the same scope.</summary>
    public Task YjsFrameAsync(CollaborationFrame frame);
}
