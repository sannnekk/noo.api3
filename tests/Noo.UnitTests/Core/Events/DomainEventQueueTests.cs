using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.System.Events;

namespace Noo.UnitTests.Core.Events;

public class DomainEventQueueTests
{
    private sealed class DummyEvent : IDomainEvent { }

    [Fact]
    public void TryEnqueue_Returns_False_When_Full()
    {
        var opts = Options.Create(new EventsConfig { QueueCapacity = 2 });
        var queue = new DomainEventQueue(opts);

        Assert.True(queue.TryEnqueue(new DummyEvent()));
        Assert.True(queue.TryEnqueue(new DummyEvent()));
        // Queue is at capacity; with Wait full-mode, TryEnqueue surfaces back-pressure.
        Assert.False(queue.TryEnqueue(new DummyEvent()));

        var read = 0;
        while (queue.Reader.TryRead(out _)) read++;
        Assert.Equal(2, read);
    }
}
