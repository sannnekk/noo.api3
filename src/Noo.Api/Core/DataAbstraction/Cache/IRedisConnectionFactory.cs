using StackExchange.Redis;

namespace Noo.Api.Core.DataAbstraction.Cache;

public interface IRedisConnectionFactory
{
    public IConnectionMultiplexer Connect(string connectionString);
}
