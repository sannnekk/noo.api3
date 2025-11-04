using Microsoft.EntityFrameworkCore.ChangeTracking;
// Other required namespaces (System.*, System.Linq, collections) are provided via implicit global usings.

namespace Noo.Api.Core.DataAbstraction.Db.ValueComparers;

/// <summary>
/// Central place for structural <see cref="ValueComparer"/> instances used with custom converters
/// to prevent EF Core warnings about collection/enumeration types without value comparers.
/// </summary>
public static class CollectionValueComparers
{
    public static readonly ValueComparer<Ulid[]> UlidArray = new(
        (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
        a => a == null ? 0 : a.Aggregate(17, (h, v) => unchecked(h * 31 + v.GetHashCode())),
        a => a == null ? null! : a.ToArray()
    );

    public static ValueComparer<Dictionary<string, TValue>> Dictionary<TValue>() where TValue : notnull
    {
        return new ValueComparer<Dictionary<string, TValue>>(
            (d1, d2) => ReferenceEquals(d1, d2) || (d1 != null && d2 != null && d1.Count == d2.Count && !d1.Except(d2).Any()),
            d => GetDictionaryHashCode(d),
            d => d == null ? null! : d.ToDictionary(kv => kv.Key, kv => kv.Value)
        );
    }

    private static int GetDictionaryHashCode<TValue>(Dictionary<string, TValue> d) where TValue : notnull
    {
        if (d == null) return 0;
        unchecked
        {
            var hash = 17;
            foreach (var kv in d.OrderBy(k => k.Key))
            {
                hash = hash * 31 + kv.Key.GetHashCode();
                hash = hash * 31 + (kv.Value?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
