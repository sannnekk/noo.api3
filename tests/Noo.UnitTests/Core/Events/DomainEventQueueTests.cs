using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.System.Events;

namespace Noo.UnitTests.Core.Events;

public class DomainEventQueueTests
{
    private sealed class DummyEvent : IDomainEvent { }

    private static DomainEventQueue CreateQueue(int capacity, int enqueueTimeoutSeconds = 2)
    {
        var opts = Options.Create(
            new EventsConfig { QueueCapacity = capacity, EnqueueTimeoutSeconds = enqueueTimeoutSeconds }
        );

        return new DomainEventQueue(opts, NullLogger<DomainEventQueue>.Instance);
    }

    [Fact]
    public void TryEnqueue_Returns_False_When_Full()
    {
        var queue = CreateQueue(2);

        Assert.True(queue.TryEnqueue(new DummyEvent()));
        Assert.True(queue.TryEnqueue(new DummyEvent()));
        // Queue is at capacity; with Wait full-mode, TryEnqueue surfaces back-pressure.
        Assert.False(queue.TryEnqueue(new DummyEvent()));

        var read = 0;
        while (queue.Reader.TryRead(out _)) read++;
        Assert.Equal(2, read);
    }

    [Fact]
    public async Task EnqueueAsync_Waits_For_Room_Instead_Of_Dropping()
    {
        var queue = CreateQueue(1);

        await queue.EnqueueAsync(new DummyEvent());

        // Queue is full — this call parks until the reader drains a slot.
        var pending = queue.EnqueueAsync(new DummyEvent()).AsTask();
        Assert.False(pending.IsCompleted);

        Assert.True(queue.Reader.TryRead(out _));
        await pending;

        Assert.True(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task EnqueueAsync_Drops_The_Event_When_The_Queue_Stays_Full()
    {
        var queue = CreateQueue(1, enqueueTimeoutSeconds: 1);

        await queue.EnqueueAsync(new DummyEvent());

        // Nothing drains, so the wait times out and the event is dropped rather than
        // pinning the caller indefinitely.
        await queue.EnqueueAsync(new DummyEvent());

        Assert.True(queue.Reader.TryRead(out _));
        Assert.False(queue.Reader.TryRead(out _));
    }
}
