using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.System.Events;

namespace Noo.UnitTests.Core.Events;

public class DomainEventCollectorTests
{
    private sealed record DummyEvent(int Id) : IDomainEvent;

    private static (DomainEventQueue Queue, DomainEventCollector Collector) Create()
    {
        var queue = new DomainEventQueue(
            Options.Create(new EventsConfig { QueueCapacity = 16 }),
            NullLogger<DomainEventQueue>.Instance
        );

        return (queue, new DomainEventCollector(queue));
    }

    [Fact]
    public async Task Collected_Events_Do_Not_Reach_The_Queue_Before_Flush()
    {
        var (queue, collector) = Create();
        var publisher = new InMemoryEventBus(collector);

        await publisher.PublishAsync(new DummyEvent(1));
        await publisher.PublishAsync(new DummyEvent(2));

        // The unit of work has not committed yet, so handlers must not be able to observe anything.
        Assert.False(queue.Reader.TryRead(out _));

        await collector.FlushAsync();

        Assert.True(queue.Reader.TryRead(out var first));
        Assert.True(queue.Reader.TryRead(out var second));
        Assert.Equal(new DummyEvent(1), first);
        Assert.Equal(new DummyEvent(2), second);
    }

    [Fact]
    public async Task Discard_Drops_Collected_Events()
    {
        var (queue, collector) = Create();
        var publisher = new InMemoryEventBus(collector);

        await publisher.PublishAsync(new DummyEvent(1));
        collector.Discard();

        await collector.FlushAsync();

        // The request rolled back, so its side effects must never fire.
        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Flush_Is_Idempotent()
    {
        var (queue, collector) = Create();
        var publisher = new InMemoryEventBus(collector);

        await publisher.PublishAsync(new DummyEvent(1));

        await collector.FlushAsync();
        await collector.FlushAsync();

        Assert.True(queue.Reader.TryRead(out _));
        Assert.False(queue.Reader.TryRead(out _));
    }
}
