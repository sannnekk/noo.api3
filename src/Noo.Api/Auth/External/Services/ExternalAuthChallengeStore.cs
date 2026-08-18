using Noo.Api.Auth.External.Types;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.External.Services;

[RegisterScoped(typeof(IExternalAuthChallengeStore))]
public class ExternalAuthChallengeStore : IExternalAuthChallengeStore
{
    private const string _keyPrefix = "external-auth:state:";

    /// <summary>Both providers expire the authorization code after ten minutes.</summary>
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    private readonly ICacheRepository _cache;

    public ExternalAuthChallengeStore(ICacheRepository cache)
    {
        _cache = cache;
    }

    public Task SaveAsync(ExternalAuthChallenge challenge)
    {
        return _cache.SetAsync(BuildKey(challenge.State), challenge, _ttl);
    }

    public async Task<ExternalAuthChallenge?> RedeemAsync(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return null;
        }

        var key = BuildKey(state);
        var challenge = await _cache.GetAsync<ExternalAuthChallenge>(key);

        await _cache.RemoveAsync(key);

        return challenge;
    }

    private static string BuildKey(string state) => _keyPrefix + state;
}
