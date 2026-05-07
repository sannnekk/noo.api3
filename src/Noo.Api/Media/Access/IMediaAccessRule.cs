using Noo.Api.Media.Types;

namespace Noo.Api.Media.Access;

/// <summary>
/// Extra access check applied on top of the baseline "must be authenticated" rule.
/// To add a new rule:
///   1. Implement this interface in a new class.
///   2. Decorate it with <c>[RegisterScoped(typeof(IMediaAccessRule))]</c>.
///   3. List the categories it covers in <see cref="Categories"/>.
/// Multiple rules for the same category all run; ALL of them must allow.
/// </summary>
public interface IMediaAccessRule
{
    public IReadOnlySet<MediaCategory> Categories { get; }

    public Task<MediaAccessDecision> EvaluateAsync(
        MediaAccessContext context,
        CancellationToken cancellationToken = default);
}
