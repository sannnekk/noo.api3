using System.Reflection;
using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;

namespace Noo.UnitTests.Common;

public static class MapperTestUtils
{
    public static IMapper CreateAppMapper()
    {
        var profiles = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttributes(typeof(AutoMapperProfileAttribute), false).Length != 0)
            .ToList();

        var config = new MapperConfiguration(cfg =>
        {
            foreach (var profile in profiles)
            {
                cfg.AddProfile(profile);
            }
        });

        config.AssertConfigurationIsValid();
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
