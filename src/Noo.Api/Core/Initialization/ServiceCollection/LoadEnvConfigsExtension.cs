using System.Reflection;
using Noo.Api.Core.Config;
using Noo.Api.Core.Config.Env;
using Microsoft.Extensions.Options;

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
        AddConfig<RateLimitingConfig>(services, configuration, RateLimitingConfig.SectionName);

        AddModuleConfigs(services, configuration);
    }

    private static void AddConfig<TConfig>(IServiceCollection services, IConfiguration config, string sectionName) where TConfig : class, IConfig
    {
        services.AddOptions<TConfig>()
            .Bind(config.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void AddModuleConfigs(IServiceCollection services, IConfiguration configuration)
    {
        var configTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<ModuleConfigAttribute>() != null);

        foreach (var configType in configTypes)
        {
            if (!typeof(IConfig).IsAssignableFrom(configType))
            {
                throw new InvalidOperationException($"Module config type {configType.FullName} must implement IConfig interface.");
            }

            if (!configType.IsClass || configType.IsAbstract)
            {
                throw new InvalidOperationException($"Module config type {configType.FullName} must be a non-abstract class.");
            }

            var sectionNameProperty = configType.GetProperty("SectionName", BindingFlags.Public | BindingFlags.Static);

            if (sectionNameProperty == null)
            {
                throw new InvalidOperationException($"Module config type {configType.FullName} must have a public static SectionName property.");
            }

            var sectionName = sectionNameProperty.GetValue(null) as string;

            if (string.IsNullOrEmpty(sectionName))
            {
                throw new InvalidOperationException($"Module config type {configType.FullName} must have a non-empty SectionName property.");
            }

            RegisterUnknownOptions(services, configuration, configType, sectionName);
        }
    }

    private static void RegisterUnknownOptions(
        IServiceCollection services,
        IConfiguration config,
        Type configType,
        string sectionName)
    {
        var section = config.GetSection(sectionName);

        // services.AddOptions<TConfig>()
        var addOptionsMethod = typeof(OptionsServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == nameof(OptionsServiceCollectionExtensions.AddOptions) &&
                m.IsGenericMethodDefinition &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 1);

        var optionsBuilder = addOptionsMethod
            .MakeGenericMethod(configType)
            .Invoke(null, [services]);

        if (optionsBuilder == null)
        {
            throw new InvalidOperationException($"Failed to build options for config type {configType.FullName}.");
        }

        // optionsBuilder.Bind(section)
        var bindMethod = typeof(global::Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == "Bind" &&
                m.IsGenericMethodDefinition &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType == typeof(IConfiguration));

        optionsBuilder = bindMethod
            .MakeGenericMethod(configType)
            .Invoke(null, [optionsBuilder, section]);

        if (optionsBuilder == null)
        {
            throw new InvalidOperationException($"Failed to bind configuration section '{sectionName}' for {configType.FullName}.");
        }

        // optionsBuilder.ValidateDataAnnotations()
        var validateDataAnnotationsMethod = typeof(global::Microsoft.Extensions.DependencyInjection.OptionsBuilderDataAnnotationsExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == "ValidateDataAnnotations" &&
                m.IsGenericMethodDefinition &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 1);

        optionsBuilder = validateDataAnnotationsMethod
            .MakeGenericMethod(configType)
            .Invoke(null, [optionsBuilder]);

        if (optionsBuilder == null)
        {
            throw new InvalidOperationException($"Failed to validate data annotations for {configType.FullName}.");
        }

        // optionsBuilder.ValidateOnStart() (if present)
        var validateOnStartMethod = typeof(global::Microsoft.Extensions.DependencyInjection.OptionsBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(m =>
                m.Name == "ValidateOnStart" &&
                m.IsGenericMethodDefinition &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 1);

        if (validateOnStartMethod != null)
        {
            _ = validateOnStartMethod
                .MakeGenericMethod(configType)
                .Invoke(null, [optionsBuilder]);
        }

        // Also register TConfig itself for cases where code resolves the config type directly.
        // This keeps module configs consistent with other DI patterns.
        services.AddSingleton(configType, sp =>
        {
            var iOptionsType = typeof(IOptions<>).MakeGenericType(configType);
            var options = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService(sp, iOptionsType);
            return iOptionsType.GetProperty("Value")!.GetValue(options)!;
        });
    }
}
