using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Initialization.Configuration;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class CacheProviderExtension
{
    public static void AddCacheProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheConfig = configuration.GetSection(CacheConfig.SectionName).GetOrThrow<CacheConfig>();
        var useMemoryOnly = string.Equals(cacheConfig.Provider, "Memory", StringComparison.OrdinalIgnoreCase);

        services.TryAddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.TryAddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.TryAddSingleton<MemoryCacheRepository>();

        services.AddSingleton<IDistributedCache>(sp =>
        {
            var redisProvider = sp.GetRequiredService<IRedisConnectionProvider>();
            if (!useMemoryOnly && redisProvider.TryGetConnection(out var connection))
            {
                var redisOptions = Options.Create(new RedisCacheOptions
                {
                    InstanceName = cacheConfig.Prefix,
                    ConnectionMultiplexerFactory = () => Task.FromResult(connection)
                });

                return ActivatorUtilities.CreateInstance<RedisCache>(sp, redisOptions);
            }

            return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        });

        if (useMemoryOnly)
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheRepository>(sp =>
        {
            var redisProvider = sp.GetRequiredService<IRedisConnectionProvider>();
            if (!useMemoryOnly && redisProvider.TryGetConnection(out var connection))
            {
                return ActivatorUtilities.CreateInstance<RedisCacheRepository>(sp, connection);
            }

            return sp.GetRequiredService<MemoryCacheRepository>();
        });
    }
}
