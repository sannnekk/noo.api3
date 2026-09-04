using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Realtime;

namespace Noo.IntegrationTests.Endpoints;

/// <summary>
/// The only test that can fail if the backplane is misconfigured. Every single-instance test
/// passes with no backplane at all, because the instance that publishes is also the one holding
/// the connection — which is precisely the case that does not occur in production.
/// </summary>
public class RealtimeBackplaneTests
{
    private sealed class BackplaneApiFactory : ApiFactory
    {
        private readonly string _channelPrefix;

        /// <summary>
        /// The prefix is supplied rather than generated, because instances only reach each other
        /// when they share one — two instances on different prefixes behave exactly like two
        /// instances with no backplane at all.
        /// </summary>
        public BackplaneApiFactory(string channelPrefix)
        {
            _channelPrefix = channelPrefix;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            // UseSetting rather than ConfigureAppConfiguration: AddNooRealtime reads the section
            // eagerly while Program.cs is still registering services, which is before any
            // ConfigureAppConfiguration callback has run.
            builder
                .UseSetting(
                    "Realtime:BackplaneConnectionString",
                    $"{BackplaneFactAttribute.Host}:{BackplaneFactAttribute.Port}"
                )
                .UseSetting("Realtime:ChannelPrefix", _channelPrefix);
        }
    }

    /// <summary>Isolates one test run from any leftover subscriptions of an earlier one.</summary>
    private static string NewChannelPrefix() => $"noo:rt:test:{Ulid.NewUlid()}:";

    private static HubConnection ConnectTo(ApiFactory instance, string accessToken) =>
        new HubConnectionBuilder()
            .WithUrl(
                new Uri(instance.Server.BaseAddress, "hubs/notifications"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => instance.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            )
            .Build();

    [BackplaneFact]
    public async Task DeliversToAUserConnectedToAnotherInstance()
    {
        // Two factories are two DI containers, two hub lifetime managers and two connection
        // stores — the same separation two pods have.
        var channelPrefix = NewChannelPrefix();

        await using var publishingInstance = new BackplaneApiFactory(channelPrefix);
        await using var holdingInstance = new BackplaneApiFactory(channelPrefix);

        var recipientId = Ulid.NewUlid();
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Student, recipientId);

        await using var connection = ConnectTo(holdingInstance, token);

        var received = new TaskCompletionSource<NotificationDTO>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        connection.On<NotificationDTO>(
            nameof(INotificationHubClient.NotificationCreatedAsync),
            notification => received.TrySetResult(notification)
        );

        await connection.StartAsync();

        // Published by the instance that holds no connection for this user at all.
        var response = await publishingInstance
            .CreateClient()
            .AsAdmin()
            .PostAsJsonAsync(
                "/notification",
                new BulkCreateNotificationsDTO
                {
                    UserIds = [recipientId],
                    Type = "info",
                    Title = "Across the backplane",
                    Message = "body",
                    IsBanner = false,
                }
            );

        response.EnsureSuccessStatusCode();

        var notification = await received.Task.WaitAsync(TimeSpan.FromSeconds(20));

        Assert.Equal("Across the backplane", notification.Title);
    }

    [BackplaneFact]
    public async Task ReportsTheBackplaneAsReadyWhenItIsReachable()
    {
        await using var instance = new BackplaneApiFactory(NewChannelPrefix());

        var response = await instance.CreateClient().GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Contains("realtime-backplane", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhealthy", body, StringComparison.Ordinal);
    }
}
