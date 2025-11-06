using StackExchange.Redis;

namespace Noo.Api.Core.DataAbstraction.Cache;

public class RedisConnectionFactory : IRedisConnectionFactory
{
    public IConnectionMultiplexer Connect(string connectionString)
    {
        return ConnectionMultiplexer.Connect(connectionString);
    }
}
