using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Realtime.Filters;

namespace Noo.UnitTests.Core.Realtime;

public class HubRateLimitFilterTests
{
    private static HubRateLimitFilter Create(int perMinute) =>
        new(Options.Create(new RealtimeConfig { InvocationsPerMinutePerConnection = perMinute }));

    private static HubInvocationContext ContextFor(string connectionId)
    {
        var caller = new Mock<HubCallerContext>();
        caller.SetupGet(c => c.ConnectionId).Returns(connectionId);

        return new HubInvocationContext(
            caller.Object,
            new Mock<IServiceProvider>().Object,
            new StubHub(),
            typeof(StubHub).GetMethod(nameof(StubHub.NoopAsync))!,
            []
        );
    }

    private static ValueTask<object?> Allowed(HubInvocationContext _) =>
        ValueTask.FromResult<object?>("ok");

    [Fact]
    public async Task AllowsInvocationsUpToTheLimit()
    {
        var filter = Create(perMinute: 3);
        var context = ContextFor("connection-a");

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal("ok", await filter.InvokeMethodAsync(context, Allowed));
        }
    }

    [Fact]
    public async Task RejectsTheInvocationPastTheLimit()
    {
        var filter = Create(perMinute: 2);
        var context = ContextFor("connection-a");

        await filter.InvokeMethodAsync(context, Allowed);
        await filter.InvokeMethodAsync(context, Allowed);

        await Assert.ThrowsAsync<HubException>(
            async () => await filter.InvokeMethodAsync(context, Allowed)
        );
    }

    // The budget is per connection, not per process: one noisy client must not throttle everyone.
    [Fact]
    public async Task CountsEachConnectionSeparately()
    {
        var filter = Create(perMinute: 1);

        await filter.InvokeMethodAsync(ContextFor("connection-a"), Allowed);

        Assert.Equal("ok", await filter.InvokeMethodAsync(ContextFor("connection-b"), Allowed));
    }

    private sealed class StubHub : Hub
    {
        public Task NoopAsync() => Task.CompletedTask;
    }
}
