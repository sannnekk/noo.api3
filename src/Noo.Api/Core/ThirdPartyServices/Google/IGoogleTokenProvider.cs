namespace Noo.Api.Core.ThirdPartyServices.Google;

public interface IGoogleTokenProvider
{
    /// <summary>
    /// Builds credentials from a stored refresh token. The returned <see cref="GoogleAuth"/>
    /// refreshes its own access token, so it stays valid for the whole length of an export.
    /// </summary>
    /// <exception cref="GoogleAuthRevokedException">
    /// The stored refresh token is no longer accepted by Google.
    /// </exception>
    public Task<GoogleAuth> GetAuthAsync(GoogleAuthData authData, CancellationToken ct = default);
}
