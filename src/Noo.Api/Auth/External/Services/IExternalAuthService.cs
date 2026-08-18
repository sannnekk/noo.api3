using Noo.Api.Auth.External.DTO;
using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.Services;

public interface IExternalAuthService
{
    public IReadOnlyList<ExternalAuthProviderDTO> GetProviders();

    /// <summary>
    /// Opens an authorization attempt and returns the provider URL to redirect the browser to.
    /// <paramref name="userId"/> is the linking user, or null for a login.
    /// </summary>
    public Task<string> StartAsync(
        ExternalAuthProviderType provider,
        ExternalAuthIntent intent,
        string? returnUrl,
        Ulid? userId
    );

    public Task<ExternalAuthOutcome> CompleteAsync(
        ExternalAuthProviderType provider,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default
    );

    public Task<IReadOnlyList<UserIdentityModel>> GetIdentitiesAsync(Ulid userId);

    public Task UnlinkAsync(Ulid userId, ExternalAuthProviderType provider);
}
