using System.Reflection;
using Noo.Api.Core.System.Events;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class DomainEventsExtension
{
    /// <summary>
    /// Scans the executing assembly for <see cref="IEventHandler{TEvent}"/> implementations,
    /// registers them in DI, builds the handler registry, and wires up the background dispatcher.
    /// </summary>
    public static void AddDomainEvents(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var registry = DomainEventHandlerRegistry.Build(assembly);
        services.AddSingleton(registry);

        foreach (var handlerType in registry.RegisteredHandlerTypes)
        {
            // Scoped: each dispatch creates its own scope, so each handler invocation gets a
            // fresh instance with a fresh DbContext / UnitOfWork.
            services.AddScoped(handlerType);
        }

        services.AddHostedService<DomainEventDispatcher>();
    }
}
