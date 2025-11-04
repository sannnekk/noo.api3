using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Noo.Api.Core.Config.Http;
using System.Net;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class HttpClientFactoryExtension
{
    /// <summary>
    /// Registers a default HttpClient factory + a resilient named client "DefaultExternal"
    /// for outbound calls (Google APIs, etc.). Services using IHttpClientFactory without a name
    /// will still get a basic client with handler policies applied via AddHttpClient().
    /// </summary>
    public static void AddHttpClientFactory(this IServiceCollection services)
    {
        services.AddHttpClient(); // default unnamed clients

        services.AddHttpClient("DefaultExternal")
            .ConfigureHttpClient((sp, client) =>
            {
                var cfg = sp.GetRequiredService<IOptions<HttpClientResilienceConfig>>().Value;
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(cfg.TimeoutSeconds, 2, 100));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("noo-api/1.0");
            })
            .AddPolicyHandler((sp, _) => BuildRetryPolicy(sp))
            .AddPolicyHandler((sp, _) => BuildCircuitBreakerPolicy(sp));
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IOptions<HttpClientResilienceConfig>>().Value;
        var retryCount = Math.Clamp(cfg.RetryCount, 0, 6);
        var baseDelay = TimeSpan.FromMilliseconds(Math.Clamp(cfg.RetryBaseDelayMs, 50, 2000));

        if (retryCount == 0)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(retryCount, attempt =>
                TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
            );
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IOptions<HttpClientResilienceConfig>>().Value;
        var failures = Math.Clamp(cfg.CircuitBreakerFailures, 2, 20);
        var duration = TimeSpan.FromSeconds(Math.Clamp(cfg.CircuitBreakerDurationSeconds, 5, 300));

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(failures, duration);
    }
}
