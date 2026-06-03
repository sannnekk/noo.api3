using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Initialization.Configuration;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class CorsPolicyExtension
{
    public static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var appConfig = configuration.GetSection(AppConfig.SectionName).GetOrThrow<AppConfig>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(appConfig.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    // Required so the browser sends/stores the httpOnly refresh-token cookie
                    // on cross-origin requests to /auth/refresh.
                    .AllowCredentials();
            });
        });
    }
}
