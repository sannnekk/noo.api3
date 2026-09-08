namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// How two addresses in a document relate.
///
/// A lease on a field has to block a write to the thing containing it, and the other way round:
/// removing a task while somebody is typing into it is exactly the collision leases exist to
/// prevent, and comparing paths for equality alone misses it completely.
/// </summary>
public static class CollaborationPaths
{
    /// <summary>
    /// Whether writing one path touches what the other covers: the same path, or one containing
    /// the other at a segment boundary. <c>/tasks/1</c> overlaps <c>/tasks/1/content</c> and does
    /// not overlap <c>/tasks/12</c> — which is why this compares segments rather than prefixes.
    /// </summary>
    public static bool Overlap(string path, string other) =>
        path == other || IsUnder(path, other) || IsUnder(other, path);

    private static bool IsUnder(string path, string ancestor) =>
        path.Length > ancestor.Length
        && path[ancestor.Length] == '/'
        && path.StartsWith(ancestor, StringComparison.Ordinal);
}
