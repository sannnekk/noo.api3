using Noo.Api.Auth.Services;

namespace Noo.Api.Auth.External.Types;

/// <summary>
/// What a redeemed callback turned out to be. <see cref="Session"/> is null for a link,
/// where the caller already has an open session.
/// </summary>
public record ExternalAuthOutcome(
    ExternalAuthIntent Intent,
    ExternalAuthProviderType Provider,
    string? ReturnUrl,
    AuthTokensResult? Session
);
