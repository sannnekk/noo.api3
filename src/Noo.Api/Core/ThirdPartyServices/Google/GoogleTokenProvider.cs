using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Security;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.ThirdPartyServices.Google;

[RegisterSingleton(typeof(IGoogleTokenProvider))]
public class GoogleTokenProvider : IGoogleTokenProvider
{
    private readonly GoogleConfig _config;

    private readonly ISecretProtector _secretProtector;

    public GoogleTokenProvider(IOptions<GoogleConfig> config, ISecretProtector secretProtector)
    {
        _config = config.Value;
        _secretProtector = secretProtector;
    }

    public async Task<GoogleAuth> GetAuthAsync(
        GoogleAuthData authData,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(authData.RefreshTokenEncrypted))
        {
            throw new GoogleAuthRevokedException();
        }

        string refreshToken;

        try
        {
            refreshToken = _secretProtector.Unprotect(authData.RefreshTokenEncrypted);
        }
        catch (Exception exception)
        {
            // A key rotation makes every stored token undecryptable; surface it as a
            // reconnect request rather than an opaque crypto failure.
            throw new GoogleAuthRevokedException(exception);
        }

        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _config.ClientId,
                    ClientSecret = _config.ClientSecret,
                },
                Scopes = authData.Scopes is { Length: > 0 } ? authData.Scopes : GoogleScopes.Required,
            }
        );

        var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
        var credential = new UserCredential(flow, authData.AccountEmail ?? "user", tokenResponse);

        try
        {
            // Force an initial refresh so a revoked grant fails here, before we start
            // building the export, instead of midway through writing rows.
            await credential.RefreshTokenAsync(ct);
        }
        catch (TokenResponseException exception)
        {
            throw new GoogleAuthRevokedException(exception);
        }

        return new GoogleAuth(credential);
    }
}
