using Moq;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Sessions;
using Noo.Api.Sessions.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Sessions;

public class OnlineServiceTests
{
    private static (OnlineService svc, Mock<ICacheRepository> cache, NooDbContext db) Create()
    {
        var cache = new Mock<ICacheRepository>(MockBehavior.Strict);
        var db = TestHelpers.CreateInMemoryDb();
        var svc = new OnlineService(cache.Object, db);
        return (svc, cache, db);
    }

    [Fact]
    public async Task SetSessionOnline_SetsKey()
    {
        var (svc, cache, _) = Create();
        var sessionId = Ulid.NewUlid();
        cache.Setup(c => c.SetAsync<DateTime>($"online:session:{sessionId}", It.IsAny<DateTime>(), SessionConfig.OnlineTtl))
            .Returns(Task.CompletedTask);

        await svc.SetSessionOnlineAsync(sessionId);
        cache.VerifyAll();
    }

    [Fact]
    public async Task SetUserOnline_SetsUserAndRole_WhenRoleDefined()
    {
        var (svc, cache, _) = Create();
        var userId = Ulid.NewUlid();
        const UserRoles role = UserRoles.Teacher;

        cache.Setup(c => c.SetAsync<DateTime>($"online:user:{userId}", It.IsAny<DateTime>(), SessionConfig.OnlineTtl))
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.SetAsync<DateTime>(It.Is<string>(k => k == $"online:role:{role}:{userId}"), It.IsAny<DateTime>(), SessionConfig.OnlineTtl))
            .Returns(Task.CompletedTask);

        await svc.SetUserOnlineAsync(userId, role);
        cache.VerifyAll();
    }

    [Fact]
    public async Task SetUserOnline_OnlyUser_WhenRoleInvalid()
    {
        var (svc, cache, _) = Create();
        var userId = Ulid.NewUlid();
        const UserRoles invalidRole = (UserRoles)9999;

        cache.Setup(c => c.SetAsync<DateTime>($"online:user:{userId}", It.IsAny<DateTime>(), SessionConfig.OnlineTtl))
            .Returns(Task.CompletedTask);
        await svc.SetUserOnlineAsync(userId, invalidRole);
        cache.Verify(c => c.SetAsync<DateTime>(It.Is<string>(k => k.StartsWith("online:role:")), It.IsAny<DateTime>(), SessionConfig.OnlineTtl), Times.Never);
    }

    [Fact]
    public async Task SetUserOnline_WithoutRole_OnlySetsUserKey()
    {
        var (svc, cache, _) = Create();
        var userId = Ulid.NewUlid();
        cache.Setup(c => c.SetAsync<DateTime>($"online:user:{userId}", It.IsAny<DateTime>(), SessionConfig.OnlineTtl))
            .Returns(Task.CompletedTask);

        await svc.SetUserOnlineAsync(userId);

        cache.Verify(c => c.SetAsync<DateTime>(It.Is<string>(k => k.StartsWith("online:role:")), It.IsAny<DateTime>(), SessionConfig.OnlineTtl), Times.Never);
    }

    [Fact]
    public async Task IsUserOnline_ReturnsTrue_WhenWithinTtl()
    {
        var (svc, cache, _) = Create();
        var userId = Ulid.NewUlid();
        var now = DateTime.UtcNow;
        cache.Setup(c => c.GetAsync<DateTime?>(It.Is<string>(k => k == $"online:user:{userId}")))
            .ReturnsAsync(now);

        var result = await svc.IsUserOnlineAsync(userId);
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserOnline_ReturnsFalse_WhenExpired()
    {
        var (svc, cache, _) = Create();
        var userId = Ulid.NewUlid();
        var past = DateTime.UtcNow - SessionConfig.OnlineTtl - TimeSpan.FromMinutes(1);
        cache.Setup(c => c.GetAsync<DateTime?>(It.Is<string>(k => k == $"online:user:{userId}")))
            .ReturnsAsync(past);

        var result = await svc.IsUserOnlineAsync(userId);
        Assert.False(result);
    }

    [Fact]
    public async Task GetOnlineCount_AllUsersPattern()
    {
        var (svc, cache, _) = Create();
        cache.Setup(c => c.CountAsync("online:user:*")).ReturnsAsync(10);
        var count = await svc.GetOnlineCountAsync();
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task GetOnlineCount_ByRolePattern()
    {
        var (svc, cache, _) = Create();
        cache.Setup(c => c.CountAsync("online:role:Student:*")).ReturnsAsync(7);
        var count = await svc.GetOnlineCountAsync(UserRoles.Student);
        Assert.Equal(7, count);
    }

    [Fact]
    public async Task GetOnlineCountByRoles_ReturnsDictionary()
    {
        var (svc, cache, _) = Create();
        foreach (var role in Enum.GetValues<UserRoles>())
        {
            cache.Setup(c => c.CountAsync($"online:role:{role}:*")).ReturnsAsync((int)role + 2);
        }
        var dict = await svc.GetOnlineCountByRolesAsync();
        foreach (var role in Enum.GetValues<UserRoles>())
        {
            Assert.Equal((int)role + 2, dict[role]);
        }
    }
}
