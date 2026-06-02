using Moq;
using Noo.Api.Auth;
using Noo.Api.Auth.Models;
using Noo.Api.Auth.Services;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;

namespace Noo.UnitTests.Auth;

public class EmailChangeServiceTests
{
    private sealed class Harness
    {
        public Mock<IUserRepository> Users { get; } = new();
        public Mock<ITokenService> Token { get; } = new();
        public Mock<IAuthEmailService> Email { get; } = new();
        public Mock<IAuthUrlGenerator> Url { get; } = new();

        public EmailChangeService Build() =>
            new(Users.Object, Token.Object, Email.Object, Url.Object);
    }

    [Fact]
    public async Task RequestAsync_Sends_Email()
    {
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = "hash",
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetByIdAsync(user.Id)).ReturnsAsync(user);
        h.Users.Setup(s => s.ExistsByUsernameOrEmailAsync(null, "new@example.com")).ReturnsAsync(false);
        h.Token.Setup(t => t.CreateToken(user.Id, TokenType.EmailChange, "new@example.com"))
            .Returns(new TokenModel
            {
                Token = "ectoken",
                UserId = user.Id,
                Type = TokenType.EmailChange,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        h.Url.Setup(u => u.GenerateEmailChangeUrl("ectoken")).Returns("/email-change/ectoken");
        var sent = 0;
        h.Email
            .Setup(e => e.SendEmailChangeEmailAsync("new@example.com", user.Name, "/email-change/ectoken"))
            .Callback(() => sent++)
            .Returns(Task.CompletedTask);

        var svc = h.Build();

        await svc.RequestAsync(user.Id, "new@example.com");
        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task RequestAsync_Throws_When_User_NotFound()
    {
        var h = new Harness();
        h.Users.Setup(s => s.GetByIdAsync(It.IsAny<Ulid>())).ReturnsAsync((UserModel?)null);
        var svc = h.Build();

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.RequestAsync(Ulid.NewUlid(), "new@example.com")
        );
    }

    [Fact]
    public async Task RequestAsync_Throws_When_Email_Taken()
    {
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = "hash",
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetByIdAsync(user.Id)).ReturnsAsync(user);
        h.Users.Setup(s => s.ExistsByUsernameOrEmailAsync(null, "taken@example.com")).ReturnsAsync(true);
        var svc = h.Build();

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(
            () => svc.RequestAsync(user.Id, "taken@example.com")
        );
    }

    [Fact]
    public async Task ConfirmAsync_Updates_Email_And_Deletes_Tokens()
    {
        var user = new UserModel
        {
            Id = Ulid.NewUlid(),
            Username = "john",
            Email = "john@example.com",
            Name = "John",
            PasswordHash = "hash",
            Role = UserRoles.Student,
            IsVerified = true,
            IsBlocked = false
        };

        var h = new Harness();
        h.Users.Setup(s => s.GetByIdAsync(user.Id)).ReturnsAsync(user);
        h.Token.Setup(t => t.DeleteTokens(user.Id, TokenType.EmailChange)).Verifiable();

        var svc = h.Build();

        await svc.ConfirmAsync(user.Id, "new@example.com");

        Assert.Equal("new@example.com", user.Email);
        h.Users.Verify();
        h.Token.Verify();
    }

    [Fact]
    public async Task ConfirmAsync_Throws_When_User_NotFound()
    {
        var h = new Harness();
        h.Users.Setup(s => s.GetByIdAsync(It.IsAny<Ulid>())).ReturnsAsync((UserModel?)null);
        var svc = h.Build();

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.ConfirmAsync(Ulid.NewUlid(), "new@example.com")
        );
    }
}
