using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.Services;

public interface IExternalAuthChallengeStore
{
    public Task SaveAsync(ExternalAuthChallenge challenge);

    /// <summary>
    /// Reads the challenge and drops it in the same call: a state is redeemable exactly once,
    /// so a replayed callback cannot reuse the PKCE verifier.
    /// </summary>
    public Task<ExternalAuthChallenge?> RedeemAsync(string state);
}
