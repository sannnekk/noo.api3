using System.Threading.RateLimiting;

namespace Noo.Api.Core.Security.RateLimiter;

public class LoginRateLimitPolicy : INamedRateLimitPolicy
{
    public string PolicyName => "LoginPolicy";

    public Func<HttpContext, RateLimitPartition<string>> Partitioner => context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 50,
            Window = TimeSpan.FromMinutes(1)
        });
    };
}
