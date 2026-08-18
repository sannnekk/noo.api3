using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.Services;

public interface IUsernameGenerator
{
    /// <summary>
    /// Derives a free username from what the provider told us about the account.
    /// </summary>
    public Task<string> GenerateAsync(
        ExternalUserProfile profile,
        ExternalAuthProviderType provider
    );
}
