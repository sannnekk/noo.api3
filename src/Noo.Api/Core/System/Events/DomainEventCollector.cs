using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.System.Events;

[RegisterScoped(typeof(IDomainEventCollector))]
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly DomainEventQueue _queue;
    private readonly List<IDomainEvent> _buffer = [];

    public DomainEventCollector(DomainEventQueue queue)
    {
        _queue = queue;
    }

    public void Collect(IDomainEvent @event)
    {
        _buffer.Add(@event);
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        if (_buffer.Count == 0)
        {
            return;
        }

        // Snapshot and clear first: a handler-owned scope may collect further events while
        // flushing, and those belong to the next flush rather than this one.
        var events = _buffer.ToArray();
        _buffer.Clear();

        foreach (var @event in events)
        {
            await _queue.EnqueueAsync(@event, ct);
        }
    }

    public void Discard()
    {
        _buffer.Clear();
    }
}
