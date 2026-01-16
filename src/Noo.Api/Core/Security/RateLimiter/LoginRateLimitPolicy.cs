using Noo.Api.Core.Config.Env;

namespace Noo.Api.Core.Security.RateLimiter;

public class LoginRateLimitPolicy : INamedRateLimitPolicy
{
    private readonly FixedWindowRateLimitPolicyConfig _config;

    public LoginRateLimitPolicy(FixedWindowRateLimitPolicyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public string PolicyName => "LoginPolicy";

    public Func<HttpContext, global::System.Threading.RateLimiting.RateLimitPartition<string>> Partitioner => context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return global::System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ => new global::System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = _config.PermitLimit,
            Window = TimeSpan.FromSeconds(_config.WindowSeconds),
            QueueLimit = _config.QueueLimit,
            QueueProcessingOrder = _config.QueueProcessingOrder
        });
    };
}
