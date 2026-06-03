namespace Noo.Api.Auth.Services;

public interface IRefreshTokenService
{
    /// <summary>
    /// Issues a new refresh token for the given session and persists its hash.
    /// Returns the raw token (the only place it is available in cleartext) and its expiry.
    /// </summary>
    public (string Token, DateTime ExpiresAt) IssueRefreshToken(Ulid sessionId);

    /// <summary>
    /// Validates a raw refresh token and, when valid, marks it as used (rotation).
    /// </summary>
    public Task<RefreshOutcome> RotateAsync(string rawToken);
}
