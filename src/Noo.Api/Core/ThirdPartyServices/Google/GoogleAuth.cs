using Google.Apis.Http;
using Google.Apis.Services;

namespace Noo.Api.Core.ThirdPartyServices.Google;

/// <summary>
/// Prepared Google credentials, with factory methods for service-specific clients.
/// </summary>
public readonly struct GoogleAuth
{
    private readonly IConfigurableHttpClientInitializer _credential;

    public GoogleAuth(IConfigurableHttpClientInitializer credential)
    {
        _credential = credential;
    }

    public T CreateService<T>(Func<BaseClientService.Initializer, T> factory, string applicationName)
        where T : BaseClientService
    {
        return factory(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = applicationName,
            }
        );
    }
}
