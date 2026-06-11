using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;
using IConfigurationProvider = AutoMapper.IConfigurationProvider;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class AutoMapperExtension
{
    public static void AddAutoMapperProfiles(this IServiceCollection services)
    {
        var profiles = getProfiles();

        services.AddSingleton<IConfigurationProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var config = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddGlobalIgnore("EntityName");

                    foreach (var profile in profiles)
                    {
                        cfg.AddProfile(profile);
                    }

                    cfg.AddMoscowEndOfDayNormalization();
                },
                loggerFactory
            );

            config.AssertConfigurationIsValid();
            return config;
        });

        services.AddSingleton<IMapper>(sp =>
        {
            var config = sp.GetRequiredService<IConfigurationProvider>();
            return config.CreateMapper();
        });
    }

    private static List<Type> getProfiles()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        return assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.GetCustomAttributes(typeof(AutoMapperProfileAttribute), false).Length != 0
            )
            .ToList();
    }
}
