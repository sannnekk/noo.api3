using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Noo.Api.Core.Utils.AutoMapper;
using Noo.Api.SavedTasks.Models;

namespace Noo.UnitTests.Common;

/// <summary>
/// Builds the mapper the way the app does at startup — every profile in the
/// assembly, same global ignores — and asserts it is valid. Adding a navigation
/// property to a model otherwise only shows up as a failure to boot.
/// </summary>
public class AppMapperConfigurationTests
{
    [Fact]
    public void WholeAppMapperConfigurationIsValid()
    {
        // Force the api assembly to load so the profile scan below sees it.
        _ = typeof(SavedTaskModel);

        var profiles = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(TypesOf)
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && type.GetCustomAttributes(typeof(AutoMapperProfileAttribute), false).Length != 0
            )
            .ToList();

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
            NullLoggerFactory.Instance
        );

        config.AssertConfigurationIsValid();
    }

    /// <summary>
    /// Whatever the assembly will admit to. Mocking libraries emit proxy assemblies whose
    /// types cannot always be loaded, and whether one is in the domain by the time this runs
    /// depends on which tests ran first — none of which has any bearing on the mapper.
    /// </summary>
    private static IEnumerable<Type> TypesOf(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

}
