using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Events;

/// <summary>
/// Publishes into the ambient <see cref="IDomainEventCollector"/> rather than straight onto the
/// queue, so events become visible to handlers only after the surrounding unit of work commits.
/// </summary>
[RegisterScoped(typeof(IEventPublisher))]
public class InMemoryEventBus : IEventPublisher
{
    private readonly IDomainEventCollector _collector;

    public InMemoryEventBus(IDomainEventCollector collector)
    {
        _collector = collector;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        _collector.Collect(@event);

        return Task.CompletedTask;
    }
}
