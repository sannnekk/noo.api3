using Moq;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Sessions;
using Noo.Api.Sessions.Services;

namespace Noo.UnitTests.Sessions;

public class ActiveUserServiceTests
{
    private static (ActiveUserService svc, Mock<ICacheRepository> cache) Create()
    {
        var cache = new Mock<ICacheRepository>(MockBehavior.Strict);
        var svc = new ActiveUserService(cache.Object);
        return (svc, cache);
    }

    [Fact]
    public async Task SetUserActive_SetsUserAndRoleKeys_WhenRoleDefined()
    {
        var (svc, cache) = Create();
        var userId = Ulid.NewUlid();
        const UserRoles role = UserRoles.Admin;

        cache.Setup(c => c.SetAsync<DateTime>(It.Is<string>(k => k.StartsWith("active:user:")), It.IsAny<DateTime>(), SessionConfig.ActiveTtl))
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.SetAsync<DateTime>(It.Is<string>(k => k.StartsWith("active:role:")), It.IsAny<DateTime>(), SessionConfig.ActiveTtl))
            .Returns(Task.CompletedTask);

        await svc.SetUserActiveAsync(userId, role);

        cache.VerifyAll();
    }

    [Fact]
    public async Task SetUserActive_OnlyUserKey_WhenRoleInvalidEnumValue()
    {
        var (svc, cache) = Create();
        var userId = Ulid.NewUlid();
        const UserRoles invalidRole = (UserRoles)9999; // Enum.IsDefined will be false

        cache.Setup(c => c.SetAsync<DateTime>(It.Is<string>(k => k.StartsWith("active:user:")), It.IsAny<DateTime>(), SessionConfig.ActiveTtl))
            .Returns(Task.CompletedTask);

        await svc.SetUserActiveAsync(userId, invalidRole);

        cache.Verify(c => c.SetAsync<DateTime>(It.Is<string>(k => k.StartsWith("active:role:")), It.IsAny<DateTime>(), SessionConfig.ActiveTtl), Times.Never);
    }

    [Fact]
    public async Task IsUserActive_ReturnsTrue_WhenLastWithinTtl()
    {
        var (svc, cache) = Create();
        var userId = Ulid.NewUlid();
        var now = DateTime.UtcNow;

        cache.Setup(c => c.GetAsync<DateTime?>(It.Is<string>(k => k == $"active:user:{userId}")))
            .ReturnsAsync(now);

        var result = await svc.IsUserActiveAsync(userId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserActive_ReturnsFalse_WhenLastOutsideTtl()
    {
        var (svc, cache) = Create();
        var userId = Ulid.NewUlid();
        var past = DateTime.UtcNow - SessionConfig.ActiveTtl - TimeSpan.FromMinutes(1);

        cache.Setup(c => c.GetAsync<DateTime?>(It.Is<string>(k => k == $"active:user:{userId}")))
            .ReturnsAsync(past);

        var result = await svc.IsUserActiveAsync(userId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetActiveCountAsync_AllUsers_UsesPattern()
    {
        var (svc, cache) = Create();
        cache.Setup(c => c.CountAsync("active:user:*")).ReturnsAsync(5);

        var count = await svc.GetActiveCountAsync();

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetActiveCountAsync_ByRole_UsesPattern()
    {
        var (svc, cache) = Create();
        cache.Setup(c => c.CountAsync("active:role:Admin:*")).ReturnsAsync(3);

        var count = await svc.GetActiveCountAsync(UserRoles.Admin);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetActiveCountByRoles_ReturnsCountsForEachRole()
    {
        var (svc, cache) = Create();
        foreach (var role in Enum.GetValues<UserRoles>())
        {
            cache.Setup(c => c.CountAsync($"active:role:{role}:*")).ReturnsAsync((int)role + 1); // unique value per role
        }

        var dict = await svc.GetActiveCountByRolesAsync();

        foreach (var role in Enum.GetValues<UserRoles>())
        {
            Assert.Equal((int)role + 1, dict[role]);
        }
    }
}
