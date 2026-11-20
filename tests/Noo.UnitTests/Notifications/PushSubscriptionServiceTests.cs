using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Services.Delivery;
using Noo.Api.Notifications.WebPush;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Notifications;

public class PushSubscriptionServiceTests
{
    private static readonly WebPushConfig EnabledConfig = new()
    {
        Enabled = true,
        PublicKey = "public",
        PrivateKey = "private",
    };

    private static CreatePushSubscriptionDTO Dto(string endpoint) => new()
    {
        Endpoint = endpoint,
        Keys = new PushSubscriptionKeysDTO { P256dh = "p256dh", Auth = "auth" },
    };

    private static PushSubscriptionModel AddSubscription(Microsoft.EntityFrameworkCore.DbContext ctx, Ulid userId, string endpoint)
    {
        var model = new PushSubscriptionModel { UserId = userId, Endpoint = endpoint, P256dh = "k", Auth = "a" };
        ctx.Add(model);
        ctx.SaveChanges();
        return model;
    }

    [Fact]
    public async Task Subscribe_KnownEndpoint_ReassignsTheSameRow()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var original = AddSubscription(ctx, Ulid.NewUlid(), "https://push.example.com/a");
        var svc = new PushSubscriptionService(
            new PushSubscriptionRepository(ctx), Mock.Of<IWebPushSender>(), Options.Create(EnabledConfig));

        var newOwner = Ulid.NewUlid();
        var id = await svc.SubscribeAsync(newOwner, Dto("https://push.example.com/a"));
        await ctx.SaveChangesAsync();

        Assert.Equal(original.Id, id);
        var row = Assert.Single(ctx.Set<PushSubscriptionModel>());
        Assert.Same(original, row);
        Assert.Equal(newOwner, row.UserId);
    }

    [Fact]
    public async Task Subscribe_NonHttpsEndpoint_Throws()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var svc = new PushSubscriptionService(
            new PushSubscriptionRepository(ctx), Mock.Of<IWebPushSender>(), Options.Create(EnabledConfig));

        await Assert.ThrowsAsync<BadRequestException>(() => svc.SubscribeAsync(Ulid.NewUlid(), Dto("http://push.example.com/a")));
    }

    [Fact]
    public async Task SendToUser_SendsToEveryDevice_AndDropsGoneOnes()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var userId = Ulid.NewUlid();
        var alive = AddSubscription(ctx, userId, "https://push.example.com/alive");
        var gone = AddSubscription(ctx, userId, "https://push.example.com/gone");
        AddSubscription(ctx, Ulid.NewUlid(), "https://push.example.com/someone-else");

        var sender = new Mock<IWebPushSender>();
        sender.Setup(s => s.SendAsync(It.Is<PushSubscriptionModel>(m => m.Id == alive.Id), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebPushSendResult.Sent);
        sender.Setup(s => s.SendAsync(It.Is<PushSubscriptionModel>(m => m.Id == gone.Id), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebPushSendResult.Gone);

        var svc = new PushSubscriptionService(new PushSubscriptionRepository(ctx), sender.Object, Options.Create(EnabledConfig));

        await svc.SendToUserAsync(userId, new WebPushPayload { Title = "Hi" }, urgent: true);
        await ctx.SaveChangesAsync();

        sender.Verify(s => s.SendAsync(It.IsAny<PushSubscriptionModel>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.DoesNotContain(ctx.Set<PushSubscriptionModel>(), s => s.Id == gone.Id);
        Assert.Contains(ctx.Set<PushSubscriptionModel>(), s => s.Id == alive.Id);
    }

    [Fact]
    public async Task SendToUser_Disabled_SendsNothing()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var userId = Ulid.NewUlid();
        AddSubscription(ctx, userId, "https://push.example.com/a");

        var sender = new Mock<IWebPushSender>(MockBehavior.Strict);
        var svc = new PushSubscriptionService(
            new PushSubscriptionRepository(ctx), sender.Object, Options.Create(new WebPushConfig { Enabled = false }));

        await svc.SendToUserAsync(userId, new WebPushPayload { Title = "Hi" }, urgent: false);
    }

    [Fact]
    public async Task Channel_MapsNotificationToPayload()
    {
        var model = new NotificationModel
        {
            UserId = Ulid.NewUlid(),
            Type = "info",
            Title = "Title",
            Message = "Body",
            IsBanner = true,
        };

        var svc = new Mock<IPushSubscriptionService>();
        var channel = new WebPushNotificationChannel(svc.Object);

        await channel.SendAsync(model);

        svc.Verify(s => s.SendToUserAsync(
            model.UserId,
            It.Is<WebPushPayload>(p => p.Id == model.Id.ToString() && p.Title == "Title" && p.Body == "Body" && p.Type == "info"),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
