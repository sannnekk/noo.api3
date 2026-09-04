using AutoMapper;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Services;
using Noo.UnitTests.Common;
using Microsoft.AspNetCore.Http;

namespace Noo.UnitTests.Sessions;

public class SessionServiceTests
{
    private static System.Security.Claims.ClaimsPrincipal MakePrincipal(Ulid userId)
    {
        var claims = new[] { new System.Security.Claims.Claim("sub", userId.ToString()) };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }
    private static IMapper CreateMapper()
    {
        var cfg = MapperTestUtils.CreateMapperConfig(c => c.AddMaps(typeof(SessionModel).Assembly));
        return cfg.CreateMapper();
    }

    private static (SessionService svc, NooDbContext ctx, IUnitOfWork uow, ICacheRepository cache) Create()
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var mapper = CreateMapper();
        var repository = new SessionRepository(ctx);
        var cache = new MemoryCacheRepository();
        var svc = new SessionService(repository, cache, mapper);
        return (svc, ctx, uow, cache);
    }

    [Fact]
    public async Task CreateSessionIfNotExists_Creates_New_WhenNoneMatches()
    {
        var (svc, ctx, uow, _) = Create();
        var userId = Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = "Mozilla/5.0";
        http.Request.Headers["X-Device-Id"] = "dev123";
        http.User = MakePrincipal(userId);

        var id = await svc.CreateSessionIfNotExistsAsync(http, userId);
        await uow.CommitAsync();

        Assert.NotEqual(default, id);
        Assert.Equal(1, ctx.Set<SessionModel>().Count());
    }

    [Fact]
    public async Task CreateSessionIfNotExists_Updates_WhenDeviceIdMatches()
    {
        var (svc, ctx, uow, _) = Create();
        var userId = Ulid.NewUlid();
        var http1 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http1.Request.Headers.UserAgent = "Mozilla/5.0";
        http1.Request.Headers["X-Device-Id"] = "deviceX";
        http1.User = MakePrincipal(userId);

        var firstId = await svc.CreateSessionIfNotExistsAsync(http1, userId);
        await uow.CommitAsync();

        var http2 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http2.Request.Headers.UserAgent = "DifferentAgent";
        http2.Request.Headers["X-Device-Id"] = "deviceX"; // same device id
        http2.User = http1.User;

        var secondId = await svc.CreateSessionIfNotExistsAsync(http2, userId);
        await uow.CommitAsync();

        Assert.Equal(firstId, secondId); // updated existing
        Assert.Single(ctx.Set<SessionModel>());
        var session = ctx.Set<SessionModel>().First();
        Assert.Equal("DifferentAgent", session.UserAgent);
    }

    [Fact]
    public async Task CreateSessionIfNotExists_UsesUserAgentWhenNoDeviceId()
    {
        var (svc, ctx, uow, _) = Create();
        var userId = Ulid.NewUlid();
        var http1 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http1.Request.Headers.UserAgent = "AgentA";
        http1.User = MakePrincipal(userId);
        var id1 = await svc.CreateSessionIfNotExistsAsync(http1, userId);
        await uow.CommitAsync();

        var http2 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http2.Request.Headers.UserAgent = "AgentA"; // same agent
        http2.User = http1.User;
        var id2 = await svc.CreateSessionIfNotExistsAsync(http2, userId);
        await uow.CommitAsync();

        Assert.Equal(id1, id2);
        Assert.Single(ctx.Set<SessionModel>());
    }

    [Fact]
    public async Task DeleteSessionAsync_RemovesExisting()
    {
        var (svc, ctx, uow, _) = Create();
        var userId = Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = "A";
        http.User = MakePrincipal(userId);
        var sessionId = await svc.CreateSessionIfNotExistsAsync(http, userId);
        await uow.CommitAsync();

        await svc.DeleteSessionAsync(sessionId, userId);
        await uow.CommitAsync();

        Assert.Empty(ctx.Set<SessionModel>());
    }

    [Fact]
    public async Task DeleteSessionAsync_ThrowsNotFound_WhenNotOwnedOrMissing()
    {
        var (svc, _, _, _) = Create();
        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.DeleteSessionAsync(Ulid.NewUlid(), Ulid.NewUlid()));
    }

    [Fact]
    public async Task SessionExistsAsync_CachesTheHit_AndStopsQueryingTheDatabase()
    {
        var (svc, ctx, uow, cache) = Create();
        var (sessionId, userId) = await SeedSessionAsync(svc, uow);

        Assert.True(await svc.SessionExistsAsync(sessionId, userId));
        Assert.Equal(userId.ToString(), await cache.GetAsync<string>($"session:exists:{sessionId}"));

        // Removed behind the service's back: still reported as existing until the key expires.
        ctx.Set<SessionModel>().RemoveRange(ctx.Set<SessionModel>());
        await uow.CommitAsync();

        Assert.True(await svc.SessionExistsAsync(sessionId, userId));
    }

    [Fact]
    public async Task SessionExistsAsync_DoesNotLetOneUserRideAnotherUsersCachedSession()
    {
        var (svc, _, uow, _) = Create();
        var (sessionId, _) = await SeedSessionAsync(svc, uow);

        Assert.False(await svc.SessionExistsAsync(sessionId, Ulid.NewUlid()));
    }

    [Fact]
    public async Task DeleteSessionAsync_DropsTheCachedHit()
    {
        var (svc, _, uow, cache) = Create();
        var (sessionId, userId) = await SeedSessionAsync(svc, uow);

        Assert.True(await svc.SessionExistsAsync(sessionId, userId));

        await svc.DeleteSessionAsync(sessionId, userId);
        await uow.CommitAsync();

        Assert.Null(await cache.GetAsync<string>($"session:exists:{sessionId}"));
        Assert.False(await svc.SessionExistsAsync(sessionId, userId));
    }

    // Signing out everywhere backs a password reset, so a stale key here would keep another
    // device authenticated for the lifetime of the cache entry.
    [Fact]
    public async Task DeleteAllSessionsAsync_DropsEveryCachedHitForTheUser()
    {
        var (svc, _, uow, cache) = Create();
        var (firstId, userId) = await SeedSessionAsync(svc, uow, "AgentA");
        var (secondId, _) = await SeedSessionAsync(svc, uow, "AgentB", userId);

        Assert.True(await svc.SessionExistsAsync(firstId, userId));
        Assert.True(await svc.SessionExistsAsync(secondId, userId));

        await svc.DeleteAllSessionsAsync(userId);
        await uow.CommitAsync();

        Assert.Null(await cache.GetAsync<string>($"session:exists:{firstId}"));
        Assert.Null(await cache.GetAsync<string>($"session:exists:{secondId}"));
    }

    private static async Task<(Ulid sessionId, Ulid userId)> SeedSessionAsync(
        SessionService svc,
        IUnitOfWork uow,
        string userAgent = "A",
        Ulid? existingUserId = null
    )
    {
        var userId = existingUserId ?? Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = userAgent;
        http.User = MakePrincipal(userId);

        var sessionId = await svc.CreateSessionIfNotExistsAsync(http, userId);
        await uow.CommitAsync();

        return (sessionId, userId);
    }

    [Fact]
    public async Task GetSessionsAsync_ReturnsSessions()
    {
        var (svc, ctx, uow, _) = Create();
        var userId = Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = "A";
        http.User = MakePrincipal(userId);

        await svc.CreateSessionIfNotExistsAsync(http, userId);
        await uow.CommitAsync();

        var sessions = await svc.GetSessionsAsync(userId);
        Assert.Single(sessions);
    }
}
