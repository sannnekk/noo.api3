using Noo.Api.Core.Config;
using Noo.Api.Core.Config.Env;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class LoadEnvConfigsExtension
{
    public static void LoadEnvConfigs(this IServiceCollection services, IConfiguration configuration)
    {
        AddConfig<AppConfig>(services, configuration, AppConfig.SectionName);
        AddConfig<CacheConfig>(services, configuration, CacheConfig.SectionName);
        AddConfig<DbConfig>(services, configuration, DbConfig.SectionName);
        AddConfig<EmailConfig>(services, configuration, EmailConfig.SectionName);
        AddConfig<HttpConfig>(services, configuration, HttpConfig.SectionName);
        AddConfig<JwtConfig>(services, configuration, JwtConfig.SectionName);
        AddConfig<LogConfig>(services, configuration, LogConfig.SectionName);
        AddConfig<SwaggerConfig>(services, configuration, SwaggerConfig.SectionName);
        AddConfig<TelegramConfig>(services, configuration, TelegramConfig.SectionName);
        AddConfig<EventsConfig>(services, configuration, EventsConfig.SectionName);
        AddConfig<S3Config>(services, configuration, S3Config.SectionName);
        AddConfig<HttpClientResilienceConfig>(services, configuration, HttpClientResilienceConfig.SectionName);
    }

    private static void AddConfig<TConfig>(IServiceCollection services, IConfiguration config, string sectionName) where TConfig : class, IConfig
    {
        services.AddOptions<TConfig>()
            .Bind(config.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
