using Microsoft.EntityFrameworkCore;
using Moq;
using Noo.Api.Core.System.Events;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Filters;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.Services;
using Noo.Api.Notifications.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Notifications;

public class NotificationServiceTests
{
    [Fact]
    public async Task BulkCreate_Persists_And_Publishes_Events()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;

        var publisher = new Mock<IEventPublisher>(MockBehavior.Strict);
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<NotificationCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = new NotificationService(uow, new NotificationRepository(ctx), publisher.Object);

        var user1 = Ulid.NewUlid();
        var user2 = Ulid.NewUlid();
        var create = new BulkCreateNotificationsDTO
        {
            UserIds = [user1, user2],
            Type = "info",
            Title = "Hello",
            Message = "World",
            IsBanner = false,
            Link = "https://example",
            LinkText = "open",
            Channels = new[] { NotificationChannelType.Http }
        };

        await svc.BulkCreateNotificationsAsync(create);

        // Verify persisted
        var all = ctx.Set<NotificationModel>().ToList();
        Assert.Equal(2, all.Count);
        Assert.All(all, n =>
        {
            Assert.Equal("info", n.Type);
            Assert.Equal("Hello", n.Title);
            Assert.Equal("World", n.Message);
            Assert.False(n.IsRead);
        });

        // Verify events published twice
        publisher.Verify(p => p.PublishAsync(It.IsAny<NotificationCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(Skip = "EFCore InMemory doesn't support ExecuteUpdate/ExecuteDelete used by repository methods")]
    public async Task MarkAsRead_And_Delete_Work()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var publisher = new Mock<IEventPublisher>();
        var svc = new NotificationService(uow, new NotificationRepository(ctx), publisher.Object);

        var user = Ulid.NewUlid();
        await svc.BulkCreateNotificationsAsync(new BulkCreateNotificationsDTO
        {
            UserIds = [user],
            Type = "t",
            Title = "T",
            Message = "M",
            IsBanner = false
        });

        var n = await ctx.Set<NotificationModel>().Where(x => x.UserId == user).SingleAsync();
        await svc.MarkAsReadAsync(user, n.Id);

        var updated = await ctx.Set<NotificationModel>().FindAsync(n.Id);
        Assert.NotNull(updated);
        Assert.True(updated!.IsRead);

        await svc.DeleteNotificationAsync(n.Id, user);
        var deleted = await ctx.Set<NotificationModel>().FindAsync(n.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetNotifications_Returns_Filtered()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var svc = new NotificationService(uow, new NotificationRepository(ctx), new Mock<IEventPublisher>().Object);

        var u1 = Ulid.NewUlid();
        var u2 = Ulid.NewUlid();

        await svc.BulkCreateNotificationsAsync(new BulkCreateNotificationsDTO { UserIds = [u1, u1, u2], Type = "t", Title = "x", Message = "y", IsBanner = false });

        var res1 = await svc.GetNotificationsAsync(u1, new NotificationFilter { Page = 1, PerPage = 10 });
        Assert.Equal(2, res1.Total);
        Assert.Equal(2, res1.Items.Count());

        var res2 = await svc.GetNotificationsAsync(u2, new NotificationFilter { Page = 1, PerPage = 10 });
        Assert.Equal(1, res2.Total);
        Assert.Single(res2.Items);
    }
}
