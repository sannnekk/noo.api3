using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;

namespace Noo.UnitTests.Core.Events;

public class DomainEventsTests
{
    private sealed class TestEvent : IDomainEvent { public required TaskCompletionSource<bool> Tcs { get; init; } }

    [RegisterScoped(typeof(IEventHandler<TestEvent>))]
    private sealed class TestHandler : IEventHandler<TestEvent>
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
        // Minimal DI: register options, queue, dispatcher as hosted, and handler
        services.AddOptions<EventsConfig>().Configure(o => o.QueueCapacity = 8);
        services.AddSingleton<DomainEventQueue>();
        services.AddHostedService<DomainEventDispatcher>();
        services.AddScoped<IEventHandler<TestEvent>, TestHandler>();
        services.AddSingleton<IEventPublisher, InMemoryEventBus>();

        using var provider = services.BuildServiceProvider();
        // start hosted services
        foreach (var hosted in provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>())
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        var publisher = provider.GetRequiredService<IEventPublisher>();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await publisher.PublishAsync(new TestEvent { Tcs = tcs });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5))) == tcs.Task;
        Assert.True(completed);

        // stop hosted services
        foreach (var hosted in provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>())
        {
            await hosted.StopAsync(CancellationToken.None);
        }
    }
}
