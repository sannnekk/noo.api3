namespace Noo.Api.Auth.External.Types;

/// <summary>
/// The server-side half of an authorization attempt. Held in the cache under
/// <see cref="State"/> until the callback redeems it once.
/// </summary>
public sealed record ExternalAuthChallenge
{
    public required ExternalAuthProviderType Provider { get; init; }

    public required ExternalAuthIntent Intent { get; init; }

    public required string State { get; init; }

    /// <summary>PKCE verifier. Never leaves the server, which is why state is not a signed token.</summary>
    public required string CodeVerifier { get; init; }

    public required string RedirectUri { get; init; }

    /// <summary>Where the frontend should land once the callback succeeds. Relative path or null.</summary>
    public string? ReturnUrl { get; init; }

    /// <summary>The user who started a link; null for a login.</summary>
    public Ulid? UserId { get; init; }
}
