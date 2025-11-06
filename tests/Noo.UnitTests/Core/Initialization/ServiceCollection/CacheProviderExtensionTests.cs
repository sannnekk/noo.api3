using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Initialization.ServiceCollection;
using StackExchange.Redis;

namespace Noo.UnitTests.Core.Initialization.ServiceCollection;

public class CacheProviderExtensionTests
{
    private static IConfiguration BuildConfiguration(string provider)
    {
        var values = new Dictionary<string, string?>
        {
            ["Cache:Provider"] = provider,
            ["Cache:ConnectionString"] = "localhost:6379",
            ["Cache:DefaultCacheTime"] = "60",
            ["Cache:Prefix"] = "unit-test"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IServiceCollection CreateServices(IConfiguration configuration)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddOptions<CacheConfig>()
            .Bind(configuration.GetSection(CacheConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    [Fact]
    public void AddCacheProvider_UsesRedisWhenConnectionSucceeds()
    {
        // Arrange
        var configuration = BuildConfiguration("Redis");
        var services = CreateServices(configuration);

        var databaseMock = new Mock<IDatabase>();
        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);

        var connectionFactoryMock = new Mock<IRedisConnectionFactory>();
        connectionFactoryMock
            .Setup(f => f.Connect(It.IsAny<string>()))
            .Returns(multiplexerMock.Object);
        services.AddSingleton<IRedisConnectionFactory>(connectionFactoryMock.Object);

        // Act
        services.AddCacheProvider(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var cacheRepository = provider.GetRequiredService<ICacheRepository>();
        Assert.IsType<RedisCacheRepository>(cacheRepository);

        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        Assert.IsType<RedisCache>(distributedCache);

        connectionFactoryMock.Verify(f => f.Connect("localhost:6379"), Times.Once);
    }

    [Fact]
    public void AddCacheProvider_FallsBackToMemoryWhenConnectionFails()
    {
        // Arrange
        var configuration = BuildConfiguration("Redis");
        var services = CreateServices(configuration);

        var connectionFactoryMock = new Mock<IRedisConnectionFactory>();
        connectionFactoryMock
            .Setup(f => f.Connect(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Connection failed"));
        services.AddSingleton<IRedisConnectionFactory>(connectionFactoryMock.Object);

        // Act
        services.AddCacheProvider(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var cacheRepository = provider.GetRequiredService<ICacheRepository>();
        var memoryRepository = provider.GetRequiredService<MemoryCacheRepository>();
        Assert.Same(memoryRepository, cacheRepository);

        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        Assert.IsType<MemoryDistributedCache>(distributedCache);

        connectionFactoryMock.Verify(f => f.Connect("localhost:6379"), Times.Once);
    }

    [Fact]
    public void AddCacheProvider_UsesMemoryWhenConfigured()
    {
        // Arrange
        var configuration = BuildConfiguration("Memory");
        var services = CreateServices(configuration);

        var connectionFactoryMock = new Mock<IRedisConnectionFactory>();
        services.AddSingleton<IRedisConnectionFactory>(connectionFactoryMock.Object);

        // Act
        services.AddCacheProvider(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var cacheRepository = provider.GetRequiredService<ICacheRepository>();
        var memoryRepository = provider.GetRequiredService<MemoryCacheRepository>();
        Assert.Same(memoryRepository, cacheRepository);

        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        Assert.IsType<MemoryDistributedCache>(distributedCache);

        connectionFactoryMock.Verify(f => f.Connect(It.IsAny<string>()), Times.Never);
    }
}
