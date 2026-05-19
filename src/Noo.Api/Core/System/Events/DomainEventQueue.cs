using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Events;

[RegisterSingleton]
public class DomainEventQueue
{
    private readonly Channel<IDomainEvent> _channel;

    public DomainEventQueue(IOptions<EventsConfig> options)
    {
        var capacity = options.Value.QueueCapacity;

        _channel = Channel.CreateBounded<IDomainEvent>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                // Surface back-pressure to the publisher rather than silently dropping events.
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            }
        );
    }

    public bool TryEnqueue(IDomainEvent @event) => _channel.Writer.TryWrite(@event);

    public ChannelReader<IDomainEvent> Reader => _channel.Reader;
}
