using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// Represents prepared Google credentials and provides factory methods to create service-specific clients.
/// </summary>
public readonly struct GoogleAuth
{
    private readonly GoogleCredential _credential;

    public GoogleAuth(GoogleCredential credential)
    {
        _credential = credential;
    }

    public T CreateService<T>(Func<BaseClientService.Initializer, T> factory, string applicationName)
        where T : BaseClientService
    {
        return factory(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = applicationName
        });
    }

    public static GoogleCredential BuildCredential(GoogleAuthData authData, IEnumerable<string> defaultScopes)
    {
        // Service account path (preferred)
        if (!string.IsNullOrWhiteSpace(authData.ClientEmail) && !string.IsNullOrWhiteSpace(authData.PrivateKey))
        {
            var key = authData.PrivateKey!.Replace("\\n", "\n");
            return GoogleCredential.FromServiceAccountCredential(
                new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(authData.ClientEmail!)
                    {
                        Scopes = defaultScopes
                    }.FromPrivateKey(key)
                )
            );
        }

        // Fallback: user OAuth tokens. If refresh token exists we still currently rely on existing access token.
        if (!string.IsNullOrWhiteSpace(authData.AccessToken))
        {
            return GoogleCredential.FromAccessToken(authData.AccessToken!);
        }

        throw new ArgumentException("Invalid GoogleAuthData: neither service account credentials nor access token provided");
    }
}
