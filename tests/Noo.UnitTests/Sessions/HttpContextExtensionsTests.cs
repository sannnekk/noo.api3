using Microsoft.AspNetCore.Http;
using Noo.Api.Sessions.Utils;

namespace Noo.UnitTests.Sessions;

public class HttpContextExtensionsTests
{
    private static System.Security.Claims.ClaimsPrincipal MakePrincipal(Ulid userId)
    {
        var claims = new[] { new System.Security.Claims.Claim("sub", userId.ToString()) };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }
    [Fact]
    public void AsSessionModel_ParsesHeaders()
    {
        var userId = Ulid.NewUlid();
        var ctx = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1") }
        };
        ctx.Request.Headers.UserAgent = "TestAgent/1.0";
        ctx.Request.Headers["X-Device-Id"] = "device-xyz";
        ctx.User = MakePrincipal(userId);

        var model = ctx.AsSessionModel(userId);

        Assert.Equal(userId, model.UserId);
        Assert.Equal("device-xyz", model.DeviceId);
        Assert.Equal("TestAgent/1.0", model.UserAgent);
        Assert.NotNull(model.IpAddress);
        Assert.NotNull(model.LastRequestAt);
    }

    [Fact]
    public void AsSessionModel_UsesDefaults_WhenNoHeaders()
    {
        var userId = Ulid.NewUlid();
        var ctx = new DefaultHttpContext
        {
            User = MakePrincipal(userId)
        };

        var model = ctx.AsSessionModel(userId);

        Assert.Equal(userId, model.UserId);
        // DeviceId should be null when header absent
        Assert.Null(model.DeviceId);
    }
}
