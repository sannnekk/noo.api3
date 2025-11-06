using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using StackExchange.Redis;

namespace Noo.Api.Core.DataAbstraction.Cache;

public sealed class RedisConnectionProvider : IRedisConnectionProvider
{
    private readonly Lazy<IConnectionMultiplexer?> _connection;
    private readonly ILogger<RedisConnectionProvider> _logger;

    public RedisConnectionProvider(
        IOptions<CacheConfig> cacheOptions,
        IRedisConnectionFactory connectionFactory,
        ILogger<RedisConnectionProvider> logger)
    {
        _logger = logger;
        var cacheConfig = cacheOptions.Value;
        var redisEnabled = !string.Equals(cacheConfig.Provider, "Memory", StringComparison.OrdinalIgnoreCase);

        _connection = new Lazy<IConnectionMultiplexer?>(() =>
        {
            if (!redisEnabled)
            {
                _logger.LogInformation("Cache provider configured to use in-memory cache.");
                return null;
            }

            try
            {
                var multiplexer = connectionFactory.Connect(cacheConfig.ConnectionString);
                _logger.LogInformation("Connected to Redis for cache prefix {CachePrefix}.", cacheConfig.Prefix);
                return multiplexer;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to Redis at {RedisEndpoint}. Falling back to in-memory cache.", cacheConfig.ConnectionString);
                return null;
            }
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public bool TryGetConnection(out IConnectionMultiplexer connection)
    {
        var value = _connection.Value;
        connection = value!;
        return value is not null;
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated && _connection.Value is { } connection)
        {
            connection.Dispose();
        }
    }
}
