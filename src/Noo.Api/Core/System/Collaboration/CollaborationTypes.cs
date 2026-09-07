using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// Someone currently in a room. Identified by connection rather than by user: the same person in
/// two tabs is two members, and a lease belongs to the tab that took it.
/// </summary>
public record CollaborationMember
{
    [JsonPropertyName("connectionId")]
    public string ConnectionId { get; init; } = string.Empty;

    [JsonPropertyName("userId")]
    public Ulid UserId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public UserRoles Role { get; init; }

    /// <summary>
    /// Which part of the document they are looking at — a task id, for a work. Drives both the
    /// "who is where" hints in the roster and which CRDT scope their frames reach.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("joinedAt")]
    public DateTime JoinedAt { get; init; }
}

/// <summary>
/// A claim on one field. <see cref="ExpiresAt"/> is absolute and sent to every client, so a lease
/// lapses on its own on both sides — no server timer, and a client whose heartbeat stopped
/// reaching us stops believing it holds the field at the same moment we do.
/// </summary>
public record CollaborationLease
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("holder")]
    public CollaborationMember? Holder { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    public static CollaborationLease Released(string path) => new() { Path = path };
}

/// <summary>
/// One edit to the shared draft, in the vocabulary the entity's PATCH endpoint already speaks.
/// The draft is nothing but the accumulated list of these, so saving is the existing patch call.
/// </summary>
public record CollaborationOp
{
    [JsonPropertyName("op")]
    public string Op { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public JsonNode? Value { get; init; }
}

/// <summary>
/// The draft as stored: contiguous operations ending at <paramref name="Seq"/>.
/// </summary>
public record CollaborationOpLog(long Seq, IReadOnlyList<CollaborationOp> Ops)
{
    public static readonly CollaborationOpLog Empty = new(0, []);
}

/// <summary>
/// Everything a client needs to start editing: the draft on top of the entity it already
/// fetched, who else is here, and which fields are already claimed.
/// </summary>
public record CollaborationRoomState
{
    [JsonPropertyName("seq")]
    public long Seq { get; init; }

    [JsonPropertyName("version")]
    public long Version { get; init; }

    [JsonPropertyName("members")]
    public IReadOnlyList<CollaborationMember> Members { get; init; } = [];

    [JsonPropertyName("leases")]
    public IReadOnlyList<CollaborationLease> Leases { get; init; } = [];

    [JsonPropertyName("ops")]
    public IReadOnlyList<CollaborationOp> Ops { get; init; } = [];
}

/// <summary>
/// An opaque CRDT frame. The server never decodes <see cref="Payload"/> — it is base64 of a
/// y-protocols sync or awareness message, and keeping it opaque is what lets rich-text editing
/// work without a CRDT implementation on this side.
/// </summary>
public record CollaborationFrame
{
    [JsonPropertyName("scope")]
    public string Scope { get; init; } = string.Empty;

    /// <summary>"sync" or "awareness"; the client's own dispatch tag, passed through.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; init; } = string.Empty;
}

/// <summary>
/// What a client learns when it opens a scope: whether it is the one that must write the
/// starting content into the CRDT document, and who else is already there.
/// </summary>
public record CollaborationScopeState
{
    /// <summary>
    /// True for the first client into an empty scope, and only that one. Two clients both
    /// seeding an empty document is how collaborative editors end up showing everything twice.
    /// </summary>
    [JsonPropertyName("maySeed")]
    public bool MaySeed { get; init; }

    [JsonPropertyName("memberCount")]
    public int MemberCount { get; init; }
}
