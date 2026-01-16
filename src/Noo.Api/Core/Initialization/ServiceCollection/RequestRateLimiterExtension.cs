using System.Threading.RateLimiting;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.Security.RateLimiter;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class RequestRateLimiterExtension
{
    public static void AddRequestRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingConfig = configuration.GetSection(RateLimitingConfig.SectionName)
            .GetOrThrow<RateLimitingConfig>();

        services.AddRateLimiter(options =>
        {
            var globalPolicy = new GlobalRateLimitPolicy(rateLimitingConfig.Global);
            options.GlobalLimiter = PartitionedRateLimiter.Create(globalPolicy.Partitioner);

            var loginPolicy = new LoginRateLimitPolicy(rateLimitingConfig.Login);
            options.AddPolicy(loginPolicy.PolicyName, loginPolicy.Partitioner);

            var registrationPolicy = new RegistrationRateLimitPolicy(rateLimitingConfig.Registration);
            options.AddPolicy(registrationPolicy.PolicyName, registrationPolicy.Partitioner);
        });
    }
}
