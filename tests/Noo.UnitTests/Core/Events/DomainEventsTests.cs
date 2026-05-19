using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.System.Events;

namespace Noo.UnitTests.Core.Events;

public class DomainEventsTests
{
    public sealed class TestEvent : IDomainEvent
    {
        public required TaskCompletionSource<bool> Tcs { get; init; }
    }

    public sealed class TestHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken ct = default)
        {
            @event.Tcs.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Enqueued_Events_Are_Dispatched_In_Background()
    {
        var services = new ServiceCollection();
        services.AddOptions<EventsConfig>().Configure(o =>
        {
            o.QueueCapacity = 8;
            o.HandlerTimeoutSeconds = 5;
        });
        services.AddLogging();
        services.AddSingleton<DomainEventQueue>();
        services.AddSingleton(DomainEventHandlerRegistry.Build(typeof(DomainEventsTests).Assembly));
        services.AddScoped<TestHandler>();
        services.AddSingleton<IEventPublisher, InMemoryEventBus>();
        services.AddHostedService<DomainEventDispatcher>();

        using var provider = services.BuildServiceProvider();

        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        var publisher = provider.GetRequiredService<IEventPublisher>();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await publisher.PublishAsync(new TestEvent { Tcs = tcs });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5))) == tcs.Task;
        Assert.True(completed);

        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            await hosted.StopAsync(CancellationToken.None);
        }
    }
}
