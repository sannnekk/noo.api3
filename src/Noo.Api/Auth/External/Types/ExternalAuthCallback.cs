using Noo.Api.Auth.External.Exceptions;

namespace Noo.Api.Auth.External.Types;

/// <summary>
/// The callback query parameters, untouched. Kept opaque so provider-specific fields
/// (VK's <c>device_id</c>) stay inside their provider instead of leaking into shared code.
/// </summary>
public sealed record ExternalAuthCallback(IReadOnlyDictionary<string, string> Parameters)
{
    public string? Get(string key) => Parameters.TryGetValue(key, out var value) ? value : null;

    public string Require(string key) =>
        Get(key) is { Length: > 0 } value
            ? value
            : throw new ExternalAuthCallbackParameterException(key);
}
