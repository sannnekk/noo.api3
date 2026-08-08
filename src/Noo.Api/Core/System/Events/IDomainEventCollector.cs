namespace Noo.Api.Core.System.Events;

/// <summary>
/// Buffers domain events raised during a unit of work so they reach the dispatcher only once
/// that unit of work has been committed.
///
/// Without this, handlers observe uncommitted state and a request that fails after publishing
/// still leaves its side effects behind.
/// </summary>
public interface IDomainEventCollector
{
    public void Collect(IDomainEvent @event);

    /// <summary>
    /// Hands every buffered event to the queue and clears the buffer.
    /// Call only after the surrounding unit of work has committed successfully.
    /// </summary>
    public Task FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Drops every buffered event. Call when the surrounding unit of work was rolled back.
    /// </summary>
    public void Discard();
}
