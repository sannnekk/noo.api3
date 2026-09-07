namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// What one kind of document contributes to collaborative editing: who may open it, which paths
/// may be written, and how a draft becomes a saved entity. Everything else — presence, leases,
/// the op log, the CRDT relay — is the same for every document, so adding the course editor is
/// one more implementation of this and nothing in <c>Core</c> changes.
/// </summary>
public interface ICollaborationRoomHandler
{
    /// <summary>Matches the room type on the wire. Lowercase, stable: it is part of the URL.</summary>
    public string RoomType { get; }

    /// <summary>
    /// Whether the caller may edit this particular document. Role checks belong on the hub's
    /// policy; this is the per-entity answer — that it exists, and that this user may have it.
    /// </summary>
    public Task<bool> CanEditAsync(Ulid roomId, CancellationToken ct = default);

    /// <summary>
    /// Whether a path is one this document's patch endpoint would accept. Checked before an
    /// operation is allowed into the draft, so a malformed path fails for the client that sent
    /// it rather than for whoever presses Save an hour later.
    /// </summary>
    public bool IsPathAllowed(string path);

    /// <summary>
    /// Applies the accumulated draft to the entity. Implementations route this through the same
    /// patch pipeline the REST endpoint uses — the point of expressing the draft as operations is
    /// that saving needs no second way to write the entity.
    /// </summary>
    public Task SaveAsync(
        Ulid roomId,
        IReadOnlyList<CollaborationOp> ops,
        CancellationToken ct = default
    );
}
