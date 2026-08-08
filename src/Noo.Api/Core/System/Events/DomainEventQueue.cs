using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Events;

[RegisterSingleton]
public class DomainEventQueue
{
    private readonly Channel<IDomainEvent> _channel;
    private readonly ILogger<DomainEventQueue> _logger;
    private readonly TimeSpan _enqueueTimeout;

    public DomainEventQueue(IOptions<EventsConfig> options, ILogger<DomainEventQueue> logger)
    {
        var capacity = options.Value.QueueCapacity;

        _logger = logger;
        _enqueueTimeout = TimeSpan.FromSeconds(options.Value.EnqueueTimeoutSeconds);

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

    /// <summary>
    /// Enqueues an event, waiting for room if the queue is full.
    ///
    /// The fast path never allocates or yields. Only once the queue is saturated does the caller
    /// wait, and then only up to <see cref="EventsConfig.EnqueueTimeoutSeconds"/> — a sustained
    /// backlog must not pin request threads indefinitely.
    /// </summary>
    public async ValueTask EnqueueAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        if (_channel.Writer.TryWrite(@event))
        {
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_enqueueTimeout);

        try
        {
            await _channel.Writer.WriteAsync(@event, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(
                "Domain event queue stayed full for {TimeoutSeconds}s; dropping event of type {EventType}. Consider raising Events:QueueCapacity or Events:MaxConcurrentEvents.",
                _enqueueTimeout.TotalSeconds,
                @event.GetType().Name
            );
        }
    }

    public bool TryEnqueue(IDomainEvent @event) => _channel.Writer.TryWrite(@event);

    public ChannelReader<IDomainEvent> Reader => _channel.Reader;
}
