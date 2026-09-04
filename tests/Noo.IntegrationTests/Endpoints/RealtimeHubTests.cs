using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Realtime;
using Noo.Api.Core.System.Realtime.Ping;

namespace Noo.IntegrationTests.Endpoints;

public class RealtimeHubTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RealtimeHubTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Long polling rather than WebSockets: it is the transport the in-memory test server
    /// supports, and it doubles as proof that hub endpoints really are exempt from the global
    /// rate limiter — a polling transport would trip the 100/min limit within seconds.
    /// </summary>
    private HubConnection BuildConnection(string? accessToken)
    {
        var builder = new HubConnectionBuilder().WithUrl(
            new Uri(_factory.Server.BaseAddress, "hubs/ping"),
            options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();

                if (accessToken is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            }
        );

        return builder.Build();
    }

    [Fact]
    public async Task RejectsAConnectionWithNoToken()
    {
        await using var connection = BuildConnection(accessToken: null);

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task AcceptsATokenPassedInTheQueryString()
    {
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Student);

        await using var connection = BuildConnection(token);
        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    // The failure this guards against looks exactly like success: SignalR never flows an
    // HttpContext into a hub invocation, so an ICurrentUser built from IHttpContextAccessor
    // reports an anonymous user with no error at all.
    [Fact]
    public async Task ResolvesTheConnectingUserThroughICurrentUserInsideTheHub()
    {
        var userId = Ulid.NewUlid();
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Teacher, userId);

        await using var connection = BuildConnection(token);
        await connection.StartAsync();

        var pong = await connection.InvokeAsync<RealtimePong>(nameof(RealtimePingHub.PingAsync));

        Assert.Equal(userId.ToString(), pong.UserId);
        Assert.NotEmpty(pong.ConnectionId);
    }

    // Once the frontend stops polling, this registry is the only thing that knows an idle tab is
    // still there — the presence heartbeat reads it to keep the user marked online.
    [Fact]
    public async Task RegistersAndDeregistersTheConnectedUserForPresence()
    {
        var userId = Ulid.NewUlid();
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Student, userId);
        var registry = _factory.Services.GetRequiredService<RealtimeConnectionRegistry>();

        await using (var connection = BuildConnection(token))
        {
            await connection.StartAsync();
            await connection.InvokeAsync<RealtimePong>(nameof(RealtimePingHub.PingAsync));

            Assert.Contains(registry.Connected, user => user.UserId == userId);

            await connection.StopAsync();
        }

        Assert.DoesNotContain(registry.Connected, user => user.UserId == userId);
    }

    [Fact]
    public async Task DeliversAServerPushToTheCaller()
    {
        var userId = Ulid.NewUlid();
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Student, userId);

        await using var connection = BuildConnection(token);

        var pushed = new TaskCompletionSource<RealtimePong>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        connection.On<RealtimePong>(
            nameof(IRealtimePingClient.PongAsync),
            pong => pushed.TrySetResult(pong)
        );

        await connection.StartAsync();
        await connection.InvokeAsync<RealtimePong>(nameof(RealtimePingHub.PingAsync));

        var received = await pushed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(userId.ToString(), received.UserId);
    }
}
