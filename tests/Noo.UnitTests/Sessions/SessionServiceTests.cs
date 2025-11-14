using AutoMapper;
using Moq;
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
        var cfg = new MapperConfiguration(c => c.AddMaps(typeof(SessionModel).Assembly));
        return cfg.CreateMapper();
    }

    private static (SessionService svc, NooDbContext ctx, Mock<IUnitOfWork> uow) Create()
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx);
        var mapper = CreateMapper();
        var repository = new SessionRepository(ctx);
        var svc = new SessionService(uow.Object, repository, mapper);
        return (svc, ctx, uow);
    }

    [Fact]
    public async Task CreateSessionIfNotExists_Creates_New_WhenNoneMatches()
    {
        var (svc, ctx, _) = Create();
        var userId = Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = "Mozilla/5.0";
        http.Request.Headers["X-Device-Id"] = "dev123";
        http.User = MakePrincipal(userId);

        var id = await svc.CreateSessionIfNotExistsAsync(http, userId);

        Assert.NotEqual(default, id);
        Assert.Equal(1, ctx.Set<SessionModel>().Count());
    }

    [Fact]
    public async Task CreateSessionIfNotExists_Updates_WhenDeviceIdMatches()
    {
        var (svc, ctx, _) = Create();
        var userId = Ulid.NewUlid();
        var http1 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http1.Request.Headers.UserAgent = "Mozilla/5.0";
        http1.Request.Headers["X-Device-Id"] = "deviceX";
        http1.User = MakePrincipal(userId);

        var firstId = await svc.CreateSessionIfNotExistsAsync(http1, userId);

        var http2 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http2.Request.Headers.UserAgent = "DifferentAgent";
        http2.Request.Headers["X-Device-Id"] = "deviceX"; // same device id
        http2.User = http1.User;

        var secondId = await svc.CreateSessionIfNotExistsAsync(http2, userId);

        Assert.Equal(firstId, secondId); // updated existing
        Assert.Single(ctx.Set<SessionModel>());
        var session = ctx.Set<SessionModel>().First();
        Assert.Equal("DifferentAgent", session.UserAgent);
    }

    [Fact]
    public async Task CreateSessionIfNotExists_UsesUserAgentWhenNoDeviceId()
    {
        var (svc, ctx, _) = Create();
        var userId = Ulid.NewUlid();
        var http1 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http1.Request.Headers.UserAgent = "AgentA";
        http1.User = MakePrincipal(userId);
        var id1 = await svc.CreateSessionIfNotExistsAsync(http1, userId);

        var http2 = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http2.Request.Headers.UserAgent = "AgentA"; // same agent
        http2.User = http1.User;
        var id2 = await svc.CreateSessionIfNotExistsAsync(http2, userId);

        Assert.Equal(id1, id2);
        Assert.Single(ctx.Set<SessionModel>());
    }

    [Fact]
    public async Task DeleteSessionAsync_RemovesExisting()
    {
        var (svc, ctx, _) = Create();
        var userId = Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = "A";
        http.User = MakePrincipal(userId);
        var sessionId = await svc.CreateSessionIfNotExistsAsync(http, userId);

        await svc.DeleteSessionAsync(sessionId, userId);

        Assert.Empty(ctx.Set<SessionModel>());
    }

    [Fact]
    public async Task DeleteSessionAsync_ThrowsNotFound_WhenNotOwnedOrMissing()
    {
        var (svc, _, _) = Create();
        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteSessionAsync(Ulid.NewUlid(), Ulid.NewUlid()));
    }

    [Fact]
    public async Task GetSessionsAsync_ReturnsSessions()
    {
        var (svc, ctx, _) = Create();
        var userId = Ulid.NewUlid();
        var http = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        http.Request.Headers.UserAgent = "A";
        http.User = MakePrincipal(userId);

        await svc.CreateSessionIfNotExistsAsync(http, userId);

        var sessions = await svc.GetSessionsAsync(userId);
        Assert.Single(sessions);
    }
}
