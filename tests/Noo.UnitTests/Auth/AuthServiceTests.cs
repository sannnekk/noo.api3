using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Auth;
using Noo.Api.Auth.DTO;
using Noo.Api.Auth.Models;
using Noo.Api.Auth.Services;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Sessions.Services;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Users.Types;

namespace Noo.UnitTests.Auth;

public class AuthServiceTests
{
    private sealed class Harness
    {
        public Mock<ITokenService> Token { get; } = new();
        public Mock<IAuthEmailService> Email { get; } = new();
        public Mock<IAuthUrlGenerator> Url { get; } = new();
        public Mock<IEmailChangeService> EmailChange { get; } = new();
        public Mock<IUserService> Users { get; } = new();
        public IHashService Hash { get; } = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        }));
        public Mock<ISessionService> Sessions { get; } = new();
        public Mock<IRefreshTokenService> Refresh { get; } = new();
        public IHttpContextAccessor Ctx { get; } = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        public AuthService Build() => new(Token.Object, Refresh.Object, Email.Object, Url.Object, EmailChange.Object, Users.Object, Hash, Sessions.Object, Ctx);
    }

    [Fact]
    public async Task Login_HappyPath_ReturnsToken_AndUserInfo()
    {
        const string pwd = "pwd";
        var hash = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        })).Hash(pwd);
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = hash,
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetUserByUsernameOrEmailAsync(user.Username)).ReturnsAsync(user);
        h.Sessions.Setup(s => s.CreateSessionIfNotExistsAsync(It.IsAny<HttpContext>(), user.Id)).ReturnsAsync(Ulid.NewUlid());
        h.Token.Setup(t => t.GenerateAccessToken(It.IsAny<AccessTokenPayload>())).Returns(("token", DateTime.UtcNow.AddDays(1)));
        h.Refresh.Setup(r => r.IssueRefreshToken(It.IsAny<Ulid>())).Returns(("refresh", DateTime.UtcNow.AddDays(30)));
        var svc = h.Build();

        var result = await svc.LoginAsync(new LoginDTO
        {
            UsernameOrEmail = user.Username,
            Password = pwd
        });

        var resp = result.Response;
        Assert.False(string.IsNullOrWhiteSpace(resp.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal(user.Id, resp.UserInfo.Id);
        Assert.Equal(user.Username, resp.UserInfo.Username);
        Assert.Equal(user.Email, resp.UserInfo.Email);
        Assert.Equal(user.Name, resp.UserInfo.Name);

        // Ensure session creation was attempted for the user
        h.Sessions.Verify(s => s.CreateSessionIfNotExistsAsync(It.IsAny<HttpContext>(), user.Id), Times.Once);
    }

    [Fact]
    public async Task Login_Fails_When_BadPassword()
    {
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = new HashService(Options.Create(new AppConfig
            {
                Location = "test",
                BaseUrl = "http://localhost",
                UserOnlineThresholdMinutes = 15,
                UserActiveThresholdDays = 14,
                HashSecret = "secret",
                AllowedOrigins = new[] { "*" }
            })).Hash("pwd"),
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetUserByUsernameOrEmailAsync(user.Username)).ReturnsAsync(user);
        var svc = h.Build();

        await Assert.ThrowsAsync<UnauthorizedException>(() => svc.LoginAsync(new LoginDTO
        {
            UsernameOrEmail = user.Username,
            Password = "wrong"
        }));
    }

    [Fact]
    public async Task Login_Fails_When_NotVerified()
    {
        var pwd = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        })).Hash("pwd");
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = pwd,
            Role = UserRoles.Student,
            IsVerified = false,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetUserByUsernameOrEmailAsync(user.Username)).ReturnsAsync(user);
        var svc = h.Build();

        await Assert.ThrowsAsync<UserIsNotVerifiedException>(() => svc.LoginAsync(new LoginDTO
        {
            UsernameOrEmail = user.Username,
            Password = "pwd"
        }));
    }

    [Fact]
    public async Task Login_Fails_When_Blocked()
    {
        var pwd = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        })).Hash("pwd");
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = pwd,
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = true
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetUserByUsernameOrEmailAsync(user.Username)).ReturnsAsync(user);
        var svc = h.Build();

        await Assert.ThrowsAsync<UserIsBlockedException>(() => svc.LoginAsync(new LoginDTO
        {
            UsernameOrEmail = user.Username,
            Password = "pwd"
        }));
    }

    [Fact]
    public async Task Register_Sends_EmailVerification()
    {
        var newUserId = Ulid.NewUlid();
        var h = new Harness();
        h.Users.Setup(s => s.UserExistsAsync("jane", "jane@example.com")).ReturnsAsync(false);
        h.Users.Setup(s => s.CreateUser(It.IsAny<UserCreationPayload>())).Returns(newUserId);
        h.Token.Setup(t => t.CreateToken(It.IsAny<Ulid>(), TokenType.EmailVerification))
            .Returns(new TokenModel { Token = "vtoken", UserId = newUserId, Type = TokenType.EmailVerification, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        h.Url.Setup(u => u.GenerateEmailVerificationUrl("vtoken")).Returns("/verify/vtoken");
        var sent = 0;
        h.Email.Setup(e => e.SendEmailVerificationEmailAsync("jane@example.com", "Jane", "/verify/vtoken")).Callback(() => sent++).Returns(Task.CompletedTask);
        var svc = h.Build();

        await svc.RegisterAsync(new RegisterDTO
        {
            Name = "Jane",
            Username = "jane",
            Password = "pwd",
            Email = "jane@example.com"
        });

        // Our fake email service records calls; at least one should be sent
        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task RequestPasswordReset_Sends_Email()
    {
        var pwd = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        })).Hash("pwd");
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = pwd,
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetUserByUsernameOrEmailAsync(user.Email)).ReturnsAsync(user);
        h.Token.Setup(t => t.CreateToken(user.Id, TokenType.PasswordReset))
            .Returns(new TokenModel { Token = "prtoken", UserId = user.Id, Type = TokenType.PasswordReset, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        h.Url.Setup(u => u.GeneratePasswordResetUrl("prtoken")).Returns("/reset/prtoken");
        var sent = 0;
        h.Email.Setup(e => e.SendForgotPasswordEmailAsync(user.Email, user.Name, "/reset/prtoken")).Callback(() => sent++).Returns(Task.CompletedTask);
        var svc = h.Build();

        await svc.RequestPasswordResetAsync(user.Email);
        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task ConfirmPasswordReset_ChangesPassword_And_DeletesSessions()
    {
        var pwd = new HashService(Options.Create(new AppConfig
        {
            Location = "test",
            BaseUrl = "http://localhost",
            UserOnlineThresholdMinutes = 15,
            UserActiveThresholdDays = 14,
            HashSecret = "secret",
            AllowedOrigins = new[] { "*" }
        })).Hash("pwd");
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = pwd,
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Token.Setup(t => t.ValidateTokenAsync("tok")).ReturnsAsync(((Ulid?)user.Id, (TokenType?)TokenType.PasswordReset, (string?)null));
        h.Users.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        h.Sessions.Setup(s => s.DeleteAllSessions(user.Id)).Verifiable();
        h.Users.Setup(s => s.UpdateUserPasswordAsync(user.Id, It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();
        var svc = h.Build();

        await svc.ConfirmPasswordResetAsync("tok", "newpwd");
        h.Users.Verify();
        h.Sessions.Verify();
    }

    [Fact]
    public async Task ConfirmEmailChange_Delegates_To_EmailChangeService()
    {
        var userId = Ulid.NewUlid();

        var h = new Harness();
        h.Token.Setup(t => t.ValidateTokenAsync("ectok"))
            .ReturnsAsync(((Ulid?)userId, (TokenType?)TokenType.EmailChange, (string?)"new@example.com"));
        h.EmailChange.Setup(e => e.ConfirmAsync(userId, "new@example.com"))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var svc = h.Build();

        await svc.ConfirmEmailAsync("ectok");
        h.EmailChange.Verify();
    }

    [Fact]
    public async Task CheckUsername_Returns_False_When_Taken()
    {
        var h = new Harness();
        h.Users.Setup(s => s.UserExistsAsync("john", null)).ReturnsAsync(true);
        var svc = h.Build();
        var free = await svc.IsUsernameFreeAsync("john");
        Assert.False(free);
    }

    // All tests now rely on Moq for dependency behavior; no manual fakes remain.
}
