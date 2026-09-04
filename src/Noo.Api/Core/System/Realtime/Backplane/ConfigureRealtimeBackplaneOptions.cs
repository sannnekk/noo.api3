using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Noo.Api.Core.System.Realtime.Backplane;

/// <summary>
/// Points SignalR's backplane at the same multiplexer the health check pings, so the two can
/// never disagree about whether this instance is connected.
/// </summary>
public class ConfigureRealtimeBackplaneOptions : IConfigureOptions<RedisOptions>
{
    private readonly IRealtimeBackplane _backplane;
    private readonly RealtimeConfig _config;

    public ConfigureRealtimeBackplaneOptions(
        IRealtimeBackplane backplane,
        IOptions<RealtimeConfig> config
    )
    {
        _backplane = backplane;
        _config = config.Value;
    }

    public void Configure(RedisOptions options)
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal(_config.ChannelPrefix);
        options.ConnectionFactory = _ => Task.FromResult(_backplane.Connection);
    }
}
