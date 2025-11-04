using AutoMapper;
using Noo.Api.Core.Utils.AutoMapper;

namespace Noo.UnitTests.Common;

public static class MapperTestUtils
{
    public static IMapper CreateAppMapper()
    {
        var profiles = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
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
}
