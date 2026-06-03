using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Auth.Models;
using Noo.Api.Auth.Services;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Security;
using Noo.Api.Core.Utils;

namespace Noo.UnitTests.Auth;

public class RefreshTokenServiceTests
{
    private static RefreshTokenService Build(Mock<IRefreshTokenRepository> repo)
    {
        var hash = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        }));

        var jwt = Options.Create(new JwtConfig
        {
            Secret = Convert.ToBase64String(new byte[32]),
            Issuer = "test",
            Audience = "test",
            ExpireDays = 30
        });

        return new RefreshTokenService(repo.Object, hash, jwt);
    }

    [Fact]
    public async Task Rotate_Returns_Valid_And_MarksUsed_For_FreshToken()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        var stored = new RefreshTokenModel
        {
            SessionId = Ulid.NewUlid(),
            TokenHash = "ignored",
            ExpiresAt = Clock.Now.AddDays(1),
            UsedAt = null
        };
        repo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(stored);

        var outcome = await Build(repo).RotateAsync("raw");

        Assert.Equal(RefreshOutcomeStatus.Valid, outcome.Status);
        Assert.NotNull(stored.UsedAt);
    }

    [Fact]
    public async Task Rotate_Returns_Invalid_When_NotFound()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((RefreshTokenModel?)null);

        var outcome = await Build(repo).RotateAsync("raw");

        Assert.Equal(RefreshOutcomeStatus.Invalid, outcome.Status);
    }

    [Fact]
    public async Task Rotate_Returns_Invalid_When_Expired()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(new RefreshTokenModel
        {
            SessionId = Ulid.NewUlid(),
            TokenHash = "ignored",
            ExpiresAt = Clock.Now.AddDays(-1),
            UsedAt = null
        });

        var outcome = await Build(repo).RotateAsync("raw");

        Assert.Equal(RefreshOutcomeStatus.Invalid, outcome.Status);
    }

    [Fact]
    public async Task Rotate_Returns_Reused_When_AlreadyUsed()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        var stored = new RefreshTokenModel
        {
            SessionId = Ulid.NewUlid(),
            TokenHash = "ignored",
            ExpiresAt = Clock.Now.AddDays(1),
            UsedAt = Clock.Now.AddMinutes(-5)
        };
        repo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(stored);

        var outcome = await Build(repo).RotateAsync("raw");

        Assert.Equal(RefreshOutcomeStatus.Reused, outcome.Status);
        Assert.Equal(stored, outcome.Token);
    }
}
