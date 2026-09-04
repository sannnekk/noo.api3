using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Realtime;

namespace Noo.IntegrationTests.Endpoints;

public class NotificationHubTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public NotificationHubTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HubConnection BuildConnection(string accessToken) =>
        new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress, "hubs/notifications"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            )
            .Build();

    private async Task<TaskCompletionSource<NotificationDTO>> ListenAsync(
        HubConnection connection
    )
    {
        var received = new TaskCompletionSource<NotificationDTO>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        connection.On<NotificationDTO>(
            nameof(INotificationHubClient.NotificationCreatedAsync),
            notification => received.TrySetResult(notification)
        );

        await connection.StartAsync();

        return received;
    }

    private Task<HttpResponseMessage> CreateNotificationAsync(Ulid recipientId, string title) =>
        _factory
            .CreateClient()
            .AsAdmin()
            .PostAsJsonAsync(
                "/notification",
                new BulkCreateNotificationsDTO
                {
                    UserIds = [recipientId],
                    Type = "info",
                    Title = title,
                    Message = "body",
                    IsBanner = false,
                }
            );

    // The whole point of the change: a notification reaches an open tab without it asking.
    [Fact]
    public async Task PushesANewNotificationToItsRecipient()
    {
        var recipientId = Ulid.NewUlid();
        var token = TestAuthClientExtensions.AccessTokenFor(UserRoles.Student, recipientId);

        await using var connection = BuildConnection(token);
        var received = await ListenAsync(connection);

        var response = await CreateNotificationAsync(recipientId, "Pushed");
        response.EnsureSuccessStatusCode();

        var notification = await received.Task.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("Pushed", notification.Title);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task DoesNotPushANotificationToAnyoneElse()
    {
        var recipientId = Ulid.NewUlid();
        var bystanderId = Ulid.NewUlid();

        await using var recipient = BuildConnection(
            TestAuthClientExtensions.AccessTokenFor(UserRoles.Student, recipientId)
        );
        await using var bystander = BuildConnection(
            TestAuthClientExtensions.AccessTokenFor(UserRoles.Student, bystanderId)
        );

        var deliveredToRecipient = await ListenAsync(recipient);
        var deliveredToBystander = await ListenAsync(bystander);

        var response = await CreateNotificationAsync(recipientId, "Only for one");
        response.EnsureSuccessStatusCode();

        await deliveredToRecipient.Task.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.False(deliveredToBystander.Task.IsCompleted);
    }

    [Fact]
    public async Task RejectsAnUnauthenticatedConnection()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress, "hubs/notifications"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                }
            )
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }
}
