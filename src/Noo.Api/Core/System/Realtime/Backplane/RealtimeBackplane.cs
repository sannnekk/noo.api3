using Microsoft.Extensions.Options;
using Noo.Api.Core.DataAbstraction.Cache;
using StackExchange.Redis;

namespace Noo.Api.Core.System.Realtime.Backplane;

public sealed class RealtimeBackplane : IRealtimeBackplane
{
    private readonly Lazy<IConnectionMultiplexer> _connection;
    private readonly RealtimeConfig _config;

    public RealtimeBackplane(
        IOptions<RealtimeConfig> options,
        IRedisConnectionFactory connectionFactory,
        ILogger<RealtimeBackplane> logger
    )
    {
        _config = options.Value;

        _connection = new Lazy<IConnectionMultiplexer>(
            () =>
            {
                var multiplexer = connectionFactory.Connect(_config.BackplaneConnectionString!);

                logger.LogInformation(
                    "Connected to the realtime backplane with channel prefix {ChannelPrefix}.",
                    _config.ChannelPrefix
                );

                return multiplexer;
            },
            LazyThreadSafetyMode.ExecutionAndPublication
        );
    }

    public bool IsConfigured => _config.HasBackplane;

    public IConnectionMultiplexer Connection =>
        IsConfigured
            ? _connection.Value
            : throw new InvalidOperationException(
                "No realtime backplane is configured. Set Realtime:BackplaneConnectionString."
            );

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
