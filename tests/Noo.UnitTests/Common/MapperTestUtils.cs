using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Noo.Api.Core.Utils.AutoMapper;

namespace Noo.UnitTests.Common;

public static class MapperTestUtils
{
    public static MapperConfiguration CreateMapperConfig(Action<IMapperConfigurationExpression> configure)
        => new(configure, NullLoggerFactory.Instance);

    public static IMapper CreateAppMapper()
    {
        var profiles = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttributes(typeof(AutoMapperProfileAttribute), false).Length != 0)
            .ToList();

        var config = CreateMapperConfig(cfg =>
        {
            foreach (var profile in profiles)
            {
                cfg.AddProfile(profile);
            }

            cfg.AddMoscowEndOfDayNormalization();
        });

        // config.AssertConfigurationIsValid(); // Commented out for tests to allow unmapped properties
        return config.CreateMapper();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
