namespace Noo.Api.Core.ThirdPartyServices.Google;

public readonly record struct GoogleOAuthResult(string RefreshToken, string? AccountEmail);

public interface IGoogleOAuthExchangeService
{
    /// <summary>
    /// Builds the Google consent URL. Always requests offline access with a forced consent prompt,
    /// because Google only returns a refresh token on the first consent unless explicitly re-asked.
    /// </summary>
    public string BuildConsentUrl(string state);

    /// <summary>
    /// Trades a one-time authorization code for a long-lived refresh token.
    /// </summary>
    /// <exception cref="GoogleAuthRevokedException">
    /// Google rejected the code, or accepted it but returned no refresh token.
    /// </exception>
    public Task<GoogleOAuthResult> ExchangeCodeAsync(string code, CancellationToken ct = default);
}
