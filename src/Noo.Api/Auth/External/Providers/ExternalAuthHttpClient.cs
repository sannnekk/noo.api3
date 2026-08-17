namespace Noo.Api.Auth.External.Providers;

public static class ExternalAuthHttpClient
{
    /// <summary>Resilient named client (Polly retry + circuit breaker) from HttpClientFactoryExtension.</summary>
    public const string Name = "DefaultExternal";
}
