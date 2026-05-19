using System.Collections.Frozen;
using System.Reflection;

namespace Noo.Api.Core.System.Events;

/// <summary>
/// Resolves <see cref="IEventHandler{TEvent}"/> implementations for a given event type.
/// Both the handler-type map and the strongly-typed invokers are built once at startup so the
/// dispatcher hot path never reflects over the type system.
/// </summary>
public sealed class DomainEventHandlerRegistry
{
    public delegate Task HandlerInvoker(object handler, IDomainEvent @event, CancellationToken ct);

    private readonly FrozenDictionary<Type, HandlerEntry> _entries;

    private DomainEventHandlerRegistry(FrozenDictionary<Type, HandlerEntry> entries)
    {
        _entries = entries;
    }

    public static DomainEventHandlerRegistry Build(params Assembly[] assemblies)
    {
        var handlersByEvent = new Dictionary<Type, List<Type>>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IEventHandler<>))
                    {
                        continue;
                    }

                    var eventType = iface.GetGenericArguments()[0];

                    if (!handlersByEvent.TryGetValue(eventType, out var list))
                    {
                        list = new List<Type>();
                        handlersByEvent[eventType] = list;
                    }

                    list.Add(type);
                }
            }
        }

        var entries = handlersByEvent.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => new HandlerEntry(kvp.Value.ToArray(), BuildInvoker(kvp.Key))
        );

        return new DomainEventHandlerRegistry(entries);
    }

    public bool TryGet(Type eventType, out IReadOnlyList<Type> handlerTypes, out HandlerInvoker invoker)
    {
        if (_entries.TryGetValue(eventType, out var entry))
        {
            handlerTypes = entry.HandlerTypes;
            invoker = entry.Invoker;
            return true;
        }

        handlerTypes = Array.Empty<Type>();
        invoker = null!;
        return false;
    }

    public IEnumerable<Type> RegisteredHandlerTypes =>
        _entries.Values.SelectMany(e => e.HandlerTypes).Distinct();

    private static HandlerInvoker BuildInvoker(Type eventType)
    {
        // Bind the strongly-typed generic helper to the concrete event type. The resulting
        // delegate calls IEventHandler<TEvent>.HandleAsync directly (no MethodInfo.Invoke).
        var method = typeof(DomainEventHandlerRegistry)
            .GetMethod(nameof(InvokeTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType);

        return (HandlerInvoker)Delegate.CreateDelegate(typeof(HandlerInvoker), method);
    }

    private static Task InvokeTypedAsync<TEvent>(object handler, IDomainEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        return ((IEventHandler<TEvent>)handler).HandleAsync((TEvent)@event, ct);
    }

    private sealed record HandlerEntry(IReadOnlyList<Type> HandlerTypes, HandlerInvoker Invoker);
}
