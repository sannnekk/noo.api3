namespace Noo.Api.Core.System.Collaboration;

/// <summary>
/// Collapsing a draft without reading the document it applies to.
/// </summary>
public static class CollaborationOps
{
    /// <summary>
    /// Drops every operation a later one already settles: the same path written twice, or a field
    /// written and then its whole parent replaced or removed. What survives is in the order it was
    /// sent, so applying it means exactly what applying the original meant.
    ///
    /// Bounded by how much of the document was touched rather than by how long anyone was editing,
    /// which is the property compaction needs — and it gets there without loading the entity, so a
    /// handler whose document spans more than one table does not have to reassemble it first.
    /// </summary>
    public static IReadOnlyList<CollaborationOp> Deduplicate(IReadOnlyList<CollaborationOp> ops)
    {
        var kept = new List<CollaborationOp>(ops.Count);
        var settled = new List<string>(ops.Count);

        // Backwards, because it is the last write to a path that stands.
        for (var index = ops.Count - 1; index >= 0; index -= 1)
        {
            var op = ops[index];

            if (settled.Any(path => CollaborationPaths.Covers(path, op.Path)))
            {
                continue;
            }

            kept.Add(op);
            settled.Add(op.Path);
        }

        kept.Reverse();

        return kept;
    }
}
