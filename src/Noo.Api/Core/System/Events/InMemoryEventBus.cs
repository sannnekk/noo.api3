using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Events;

[RegisterSingleton(typeof(IEventPublisher))]
public class InMemoryEventBus : IEventPublisher
{
    private readonly DomainEventQueue _queue;
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(DomainEventQueue queue, ILogger<InMemoryEventBus> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        if (!_queue.TryEnqueue(@event))
        {
            // Queue is at capacity. We intentionally do not block the caller — losing visibility
            // is preferable to stalling request threads — but we surface the drop so it can be
            // monitored and the queue resized.
            _logger.LogWarning(
                "Domain event queue is full; dropping event of type {EventType}. Consider raising Events:QueueCapacity.",
                typeof(TEvent).Name
            );
        }

        return Task.CompletedTask;
    }
}
