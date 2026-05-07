using Moq;
using Noo.Api.Auth;
using Noo.Api.Auth.Models;
using Noo.Api.Auth.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using System.Security.Claims;

namespace Noo.UnitTests.Auth;

public class AuthTokenServiceTests
{
    private static (AuthTokenService svc, Mock<ITokenRepository> repo, Mock<IJwtService> jwt) CreateSvc()
    {
        var repo = new Mock<ITokenRepository>();
        var jwt = new Mock<IJwtService>();
        var uow = new Mock<IUnitOfWork>();
        var svc = new AuthTokenService(repo.Object, jwt.Object, uow.Object);
        return (svc, repo, jwt);
    }

    [Fact]
    public void CreateToken_Adds_TokenToRepository()
    {
        var (svc, repo, _) = CreateSvc();
        var userId = Ulid.NewUlid();

        var token = svc.CreateToken(userId, TokenType.EmailVerification);

        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.Equal(userId, token.UserId);
        Assert.Equal(TokenType.EmailVerification, token.Type);
        repo.Verify(
            r => r.Add(It.Is<TokenModel>(t => t.UserId == userId && t.Type == TokenType.EmailVerification)),
            Times.Once
        );
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsValues()
    {
        var (svc, repo, _) = CreateSvc();
        var userId = Ulid.NewUlid();
        var tokenModel = new TokenModel
        {
            UserId = userId,
            Type = TokenType.PasswordReset,
            Token = "abc123",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        repo.Setup(r => r.GetAsync("abc123")).ReturnsAsync(tokenModel);

        var (retUserId, retType, retPayload) = await svc.ValidateTokenAsync("abc123");

        Assert.Equal(userId, retUserId);
        Assert.Equal(TokenType.PasswordReset, retType);
        Assert.Null(retPayload);
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ReturnsNulls()
    {
        var (svc, repo, _) = CreateSvc();
        var tokenModel = new TokenModel
        {
            UserId = Ulid.NewUlid(),
            Type = TokenType.PasswordReset,
            Token = "expired",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };
        repo.Setup(r => r.GetAsync("expired")).ReturnsAsync(tokenModel);

        var (userId, type, payload) = await svc.ValidateTokenAsync("expired");

        Assert.Null(userId);
        Assert.Null(type);
        Assert.Null(payload);
    }

    [Fact]
    public async Task ValidateTokenAsync_MissingToken_ReturnsNulls()
    {
        var (svc, repo, _) = CreateSvc();
        repo.Setup(r => r.GetAsync("missing")).ReturnsAsync((TokenModel?)null);

        var (userId, type, payload) = await svc.ValidateTokenAsync("missing");

        Assert.Null(userId);
        Assert.Null(type);
        Assert.Null(payload);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsToken_FromJwtService()
    {
        var (svc, _, jwt) = CreateSvc();
        var expiry = DateTime.UtcNow.AddDays(1);
        jwt.Setup(j => j.GenerateToken(It.IsAny<IEnumerable<Claim>>()))
           .Returns(("jwt-token", expiry));

        var (token, at) = svc.GenerateAccessToken(new AccessTokenPayload
        {
            UserId = Ulid.NewUlid(),
            SessionId = Ulid.NewUlid(),
            UserRole = UserRoles.Student,
        });

        Assert.Equal("jwt-token", token);
        Assert.Equal(expiry, at);
    }
}
