
using StackExchange.Redis;

namespace Noo.Api.Core.DataAbstraction.Cache;

public interface IRedisConnectionProvider : IDisposable
{
    public bool TryGetConnection(out IConnectionMultiplexer connection);
}
