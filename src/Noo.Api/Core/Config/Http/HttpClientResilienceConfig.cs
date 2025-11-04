namespace Noo.Api.Core.Config.Http;

public class HttpClientResilienceConfig : IConfig
{
    public static string SectionName => "HttpClient";

    // Default overall request timeout (per request) in seconds
    public int TimeoutSeconds { get; set; } = 10;

    // Number of transient retries (e.g., 5xx, 408, network errors)
    public int RetryCount { get; set; } = 3;

    // Exponential backoff base delay in milliseconds
    public int RetryBaseDelayMs { get; set; } = 200;

    // Circuit breaker: number of consecutive failures before breaking
    public int CircuitBreakerFailures { get; set; } = 5;

    // Circuit breaker duration in seconds
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
