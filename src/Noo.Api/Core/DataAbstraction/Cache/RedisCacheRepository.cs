
using System.Text.Json;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using StackExchange.Redis;

namespace Noo.Api.Core.DataAbstraction.Cache;

public class RedisCacheRepository : ICacheRepository
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly CacheConfig _cacheConfig;

    public RedisCacheRepository(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CacheConfig> cacheConfig
    )
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
        _cacheConfig = cacheConfig.Value;
    }

    public async Task<int> CountAsync(string pattern = "*")
    {
        if (string.IsNullOrWhiteSpace(pattern)) pattern = "*";

        // Collect all primary (non-replica) servers (cluster or sentinel aware)
        var servers = _connectionMultiplexer
            .GetEndPoints()
            .Select(ep => _connectionMultiplexer.GetServer(ep))
            .Where(s => !s.IsReplica)
            .ToArray();

        // Fast path: count all keys -> use DBSIZE per primary (O(1) each)
        if (pattern == "*")
        {
            long total = 0;
            foreach (var s in servers)
            {
                // DatabaseSize is synchronous but very fast (reads an internal counter)
                total += s.DatabaseSize(_database.Database);
            }
            // Guard against overflow into int (extremely unlikely in practice for Redis usage here)
            if (total > int.MaxValue) return int.MaxValue;
            return (int)total;
        }

        // For pattern counts Redis has no native constant-time operation. We SCAN in parallel
        // across primaries. KeysAsync uses SCAN under the hood (non-blocking, incremental).
        // We aggregate counts per server to minimize contention.
        var totalCount = 0;
        var tasks = new List<Task>(servers.Length);

        foreach (var server in servers)
        {
            tasks.Add(Task.Run(async () =>
            {
                var local = 0;
                await foreach (var _ in server.KeysAsync(
                    database: _database.Database,
                    pattern: pattern,
                    pageSize: 5000)) // larger page size reduces round-trips
                {
                    local++;
                }
                Interlocked.Add(ref totalCount, local);
            }));
        }

        await Task.WhenAll(tasks);
        return totalCount;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        // Retrieve value from Redis
        var redisValue = await _database.StringGetAsync(key);

        // Return default if key doesn't exist
        if (redisValue.IsNullOrEmpty)
            return default;

        // Deserialize JSON to requested type
        return JsonSerializer.Deserialize<T>(redisValue.ToString());
    }

    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        string serializedValue = JsonSerializer.Serialize(value);

        // Set value in Redis with optional expiration
        await _database.StringSetAsync(
            key,
            serializedValue,
            expiry: expiration ?? _cacheConfig.DefaultCacheTimeSpan
        );
    }
}
