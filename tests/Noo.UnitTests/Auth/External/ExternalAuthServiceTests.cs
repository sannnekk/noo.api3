using Moq;
using Noo.Api.Auth.DTO;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Services;
using Noo.Api.Auth.External.Types;
using Noo.Api.Auth.Services;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Users.Types;

namespace Noo.UnitTests.Auth.External;

public class ExternalAuthServiceTests
{
    private const string _subjectId = "provider-subject-1";
    private const string _state = "state-token";
    private const string _email = "person@example.com";

    private sealed class Harness
    {
        public Mock<IExternalAuthProvider> Provider { get; } = new();
        public Mock<IExternalAuthProviderRegistry> Registry { get; } = new();
        public Mock<IExternalAuthChallengeStore> Challenges { get; } = new();
        public Mock<IUserIdentityRepository> Identities { get; } = new();
        public Mock<IUserRepository> Users { get; } = new();
        public Mock<IUserAvatarRepository> Avatars { get; } = new();
        public Mock<IUserService> UserService { get; } = new();
        public Mock<IUsernameGenerator> Usernames { get; } = new();
        public Mock<IAuthService> Auth { get; } = new();
        public Mock<IAuthUrlGenerator> Urls { get; } = new();

        public List<UserIdentityModel> Added { get; } = [];

        public Harness()
        {
            Provider.SetupGet(p => p.Type).Returns(ExternalAuthProviderType.Yandex);
            Provider.SetupGet(p => p.IsEnabled).Returns(true);
            Provider.SetupGet(p => p.EmailIsTrusted).Returns(true);
            Registry.Setup(r => r.Get(It.IsAny<ExternalAuthProviderType>())).Returns(Provider.Object);
            Identities
                .Setup(r => r.Add(It.IsAny<UserIdentityModel>()))
                .Callback<UserIdentityModel>(Added.Add);
            Identities
                .Setup(r => r.GetByUserAsync(It.IsAny<Ulid>()))
                .ReturnsAsync((IReadOnlyList<UserIdentityModel>)[]);
            Usernames
                .Setup(g => g.GenerateAsync(It.IsAny<ExternalUserProfile>(), It.IsAny<ExternalAuthProviderType>()))
                .ReturnsAsync("generated");
            Urls
                .Setup(u => u.GenerateExternalAuthCallbackUrl(It.IsAny<ExternalAuthProviderType>()))
                .Returns("http://localhost/auth/callback/yandex");
            Auth
                .Setup(a => a.IssueSessionAsync(It.IsAny<UserModel>()))
                .ReturnsAsync(new AuthTokensResult(new LoginResponseDTO(), "refresh", DateTime.UtcNow));
            UserService
                .Setup(s => s.CreateUserAsync(It.IsAny<UserCreationPayload>()))
                .ReturnsAsync((UserCreationPayload payload) => new UserModel
                {
                    Username = payload.Username,
                    Email = payload.Email,
                    Name = payload.Name,
                    Role = payload.Role,
                });
        }

        public void WithChallenge(ExternalAuthChallenge? challenge)
        {
            Challenges.Setup(s => s.RedeemAsync(_state)).ReturnsAsync(challenge);
        }

        public void WithProfile(ExternalUserProfile profile)
        {
            Provider
                .Setup(p => p.ResolveProfileAsync(
                    It.IsAny<ExternalAuthCallback>(),
                    It.IsAny<ExternalAuthChallenge>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(profile);
        }

        public ExternalAuthService Build() =>
            new(
                Registry.Object,
                Challenges.Object,
                Identities.Object,
                Users.Object,
                Avatars.Object,
                UserService.Object,
                Usernames.Object,
                Auth.Object,
                Urls.Object
            );
    }

    private static ExternalAuthChallenge Challenge(
        ExternalAuthIntent intent = ExternalAuthIntent.Login,
        ExternalAuthProviderType provider = ExternalAuthProviderType.Yandex,
        Ulid? userId = null
    ) =>
        new()
        {
            Provider = provider,
            Intent = intent,
            State = _state,
            CodeVerifier = "verifier",
            RedirectUri = "http://localhost/auth/callback/yandex",
            UserId = userId,
        };

    private static ExternalUserProfile Profile(string? email = null, bool emailIsVerified = true) =>
        new()
        {
            SubjectId = _subjectId,
            Email = email,
            EmailIsVerified = emailIsVerified,
            DisplayName = "Иван Петров",
        };

    private static UserModel User(UserRoles role = UserRoles.Student, string? email = _email) =>
        new()
        {
            Id = Ulid.NewUlid(),
            Username = "person",
            Email = email,
            Name = "Person",
            Role = role,
        };

    private static Dictionary<string, string> Callback() =>
        new() { ["state"] = _state, ["code"] = "auth-code" };

    [Fact]
    public async Task Start_Stores_A_Single_Use_Challenge_And_Returns_The_Provider_Url()
    {
        var harness = new Harness();
        ExternalAuthChallenge? saved = null;

        harness.Challenges
            .Setup(s => s.SaveAsync(It.IsAny<ExternalAuthChallenge>()))
            .Callback<ExternalAuthChallenge>(challenge => saved = challenge)
            .Returns(Task.CompletedTask);
        harness.Provider
            .Setup(p => p.BuildAuthorizationUrl(It.IsAny<ExternalAuthChallenge>(), It.IsAny<string>()))
            .Returns("https://provider.test/authorize");

        var url = await harness.Build().StartAsync(
            ExternalAuthProviderType.Yandex,
            ExternalAuthIntent.Login,
            "/courses",
            userId: null
        );

        Assert.Equal("https://provider.test/authorize", url);
        Assert.NotNull(saved);
        Assert.Equal("/courses", saved!.ReturnUrl);
        Assert.False(string.IsNullOrWhiteSpace(saved.State));
        Assert.False(string.IsNullOrWhiteSpace(saved.CodeVerifier));
        Assert.Equal("http://localhost/auth/callback/yandex", saved.RedirectUri);
    }

    [Theory]
    [InlineData("https://evil.test/steal")]
    [InlineData("//evil.test/steal")]
    [InlineData("/courses\\@evil.test")]
    [InlineData("courses")]
    public async Task Start_Rejects_A_Return_Url_That_Could_Leave_The_Site(string returnUrl)
    {
        var harness = new Harness();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            harness.Build().StartAsync(
                ExternalAuthProviderType.Yandex,
                ExternalAuthIntent.Login,
                returnUrl,
                userId: null
            )
        );

        harness.Challenges.Verify(s => s.SaveAsync(It.IsAny<ExternalAuthChallenge>()), Times.Never);
    }

    [Fact]
    public async Task Complete_Rejects_An_Unknown_State()
    {
        var harness = new Harness();
        harness.WithChallenge(null);

        await Assert.ThrowsAsync<ExternalAuthStateInvalidException>(() =>
            harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback())
        );
    }

    [Fact]
    public async Task Complete_Rejects_A_Challenge_Opened_For_Another_Provider()
    {
        var harness = new Harness();
        harness.WithChallenge(Challenge(provider: ExternalAuthProviderType.Vk));

        await Assert.ThrowsAsync<ExternalAuthStateInvalidException>(() =>
            harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback())
        );
    }

    [Fact]
    public async Task Complete_Rejects_A_Callback_Without_State()
    {
        var harness = new Harness();

        await Assert.ThrowsAsync<ExternalAuthCallbackParameterException>(() =>
            harness.Build().CompleteAsync(
                ExternalAuthProviderType.Yandex,
                new Dictionary<string, string> { ["code"] = "auth-code" }
            )
        );
    }

    [Fact]
    public async Task Complete_Signs_In_A_Known_Identity_Without_Creating_A_User()
    {
        var harness = new Harness();
        var user = User();
        var identity = new UserIdentityModel
        {
            Provider = ExternalAuthProviderType.Yandex,
            SubjectId = _subjectId,
            UserId = user.Id,
            User = user,
        };

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile(_email));
        harness.Identities
            .Setup(r => r.GetByProviderAndSubjectAsync(ExternalAuthProviderType.Yandex, _subjectId))
            .ReturnsAsync(identity);

        var outcome = await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.NotNull(outcome.Session);
        Assert.NotNull(identity.LastLoginAt);
        harness.UserService.Verify(s => s.CreateUserAsync(It.IsAny<UserCreationPayload>()), Times.Never);
        harness.Auth.Verify(a => a.IssueSessionAsync(user), Times.Once);
    }

    [Fact]
    public async Task Complete_Links_A_Verified_Email_To_An_Existing_Student()
    {
        var harness = new Harness();
        var user = User(UserRoles.Student);

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile(_email));
        harness.Users.Setup(r => r.GetByEmailAsync(_email)).ReturnsAsync(user);

        var outcome = await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.NotNull(outcome.Session);
        harness.UserService.Verify(s => s.CreateUserAsync(It.IsAny<UserCreationPayload>()), Times.Never);
        Assert.Equal(user.Id, Assert.Single(harness.Added).UserId);
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Teacher)]
    [InlineData(UserRoles.Assistant)]
    public async Task Complete_Refuses_To_Claim_A_Privileged_Account_By_Email(UserRoles role)
    {
        var harness = new Harness();

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile(_email));
        harness.Users.Setup(r => r.GetByEmailAsync(_email)).ReturnsAsync(User(role));

        await Assert.ThrowsAsync<ExternalAuthEmailConflictException>(() =>
            harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback())
        );

        Assert.Empty(harness.Added);
    }

    [Fact]
    public async Task Complete_Refuses_To_Claim_An_Account_When_The_Provider_Email_Is_Untrusted()
    {
        var harness = new Harness();
        harness.Provider.SetupGet(p => p.EmailIsTrusted).Returns(false);

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile(_email));
        harness.Users.Setup(r => r.GetByEmailAsync(_email)).ReturnsAsync(User());

        await Assert.ThrowsAsync<ExternalAuthEmailConflictException>(() =>
            harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback())
        );
    }

    [Fact]
    public async Task Complete_Creates_A_Verified_Account_For_An_Unknown_Identity()
    {
        var harness = new Harness();

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile(email: null));

        var outcome = await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.NotNull(outcome.Session);
        harness.UserService.Verify(
            s => s.CreateUserAsync(It.Is<UserCreationPayload>(p =>
                p.Username == "generated" && p.Email == null && p.Role == UserRoles.Student
            )),
            Times.Once
        );
        Assert.Equal(_subjectId, Assert.Single(harness.Added).SubjectId);
    }

    [Fact]
    public async Task Complete_Marks_A_Created_Account_Verified_Because_The_Provider_Vouched_For_It()
    {
        var harness = new Harness();
        UserModel? issued = null;

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile());
        harness.Auth
            .Setup(a => a.IssueSessionAsync(It.IsAny<UserModel>()))
            .Callback<UserModel>(user => issued = user)
            .ReturnsAsync(new AuthTokensResult(new LoginResponseDTO(), "refresh", DateTime.UtcNow));

        await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.True(issued!.IsVerified);
    }

    [Fact]
    public async Task Complete_Propagates_The_Block_Check_From_Session_Issuance()
    {
        var harness = new Harness();

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile());
        harness.Auth
            .Setup(a => a.IssueSessionAsync(It.IsAny<UserModel>()))
            .ThrowsAsync(new UserIsBlockedException());

        await Assert.ThrowsAsync<UserIsBlockedException>(() =>
            harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback())
        );
    }

    [Fact]
    public async Task Complete_Stores_A_Provider_Avatar_For_A_New_Account()
    {
        var harness = new Harness();
        UserAvatarModel? added = null;

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile() with { AvatarUrl = "https://avatars.test/picture.png" });
        harness.Avatars
            .Setup(r => r.Add(It.IsAny<UserAvatarModel>()))
            .Callback<UserAvatarModel>(avatar => added = avatar);

        await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.NotNull(added);
        Assert.Equal(UserAvatarType.External, added!.AvatarType);
        Assert.Equal("https://avatars.test/picture.png", added.AvatarUrl);
    }

    [Fact]
    public async Task Complete_Never_Overwrites_An_Uploaded_Avatar()
    {
        var harness = new Harness();
        var user = User();
        var avatar = new UserAvatarModel
        {
            UserId = user.Id,
            AvatarType = UserAvatarType.Custom,
            AvatarUrl = string.Empty,
        };

        harness.WithChallenge(Challenge());
        harness.WithProfile(Profile(_email) with { AvatarUrl = "https://avatars.test/picture.png" });
        harness.Users.Setup(r => r.GetByEmailAsync(_email)).ReturnsAsync(user);
        harness.Avatars.Setup(r => r.GetUserAvatarByUserIdAsync(user.Id)).ReturnsAsync(avatar);

        await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.Equal(UserAvatarType.Custom, avatar.AvatarType);
        Assert.Equal(string.Empty, avatar.AvatarUrl);
    }

    [Fact]
    public async Task Complete_Links_A_Provider_To_The_Account_That_Started_The_Link()
    {
        var harness = new Harness();
        var userId = Ulid.NewUlid();

        harness.WithChallenge(Challenge(ExternalAuthIntent.Link, userId: userId));
        harness.WithProfile(Profile(_email));

        var outcome = await harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback());

        Assert.Equal(ExternalAuthIntent.Link, outcome.Intent);
        Assert.Null(outcome.Session);
        Assert.Equal(userId, Assert.Single(harness.Added).UserId);
        harness.Auth.Verify(a => a.IssueSessionAsync(It.IsAny<UserModel>()), Times.Never);
    }

    [Fact]
    public async Task Complete_Refuses_To_Link_A_Subject_Owned_By_Another_Account()
    {
        var harness = new Harness();
        var owner = User();

        harness.WithChallenge(Challenge(ExternalAuthIntent.Link, userId: Ulid.NewUlid()));
        harness.WithProfile(Profile(_email));
        harness.Identities
            .Setup(r => r.GetByProviderAndSubjectAsync(ExternalAuthProviderType.Yandex, _subjectId))
            .ReturnsAsync(new UserIdentityModel
            {
                Provider = ExternalAuthProviderType.Yandex,
                SubjectId = _subjectId,
                UserId = owner.Id,
                User = owner,
            });

        await Assert.ThrowsAsync<ExternalIdentityAlreadyLinkedException>(() =>
            harness.Build().CompleteAsync(ExternalAuthProviderType.Yandex, Callback())
        );
    }

    [Fact]
    public async Task Unlink_Removes_The_Identity()
    {
        var harness = new Harness();
        var user = User();
        var identity = new UserIdentityModel
        {
            Provider = ExternalAuthProviderType.Yandex,
            SubjectId = _subjectId,
            UserId = user.Id,
        };

        user.PasswordHash = "hash";
        harness.Identities
            .Setup(r => r.GetByUserAsync(user.Id))
            .ReturnsAsync((IReadOnlyList<UserIdentityModel>)[identity]);
        harness.Users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await harness.Build().UnlinkAsync(user.Id, ExternalAuthProviderType.Yandex);

        harness.Identities.Verify(r => r.Delete(identity), Times.Once);
    }

    [Fact]
    public async Task Unlink_Refuses_To_Remove_The_Only_Way_Into_The_Account()
    {
        var harness = new Harness();
        var user = User();
        var identity = new UserIdentityModel
        {
            Provider = ExternalAuthProviderType.Yandex,
            SubjectId = _subjectId,
            UserId = user.Id,
        };

        harness.Identities
            .Setup(r => r.GetByUserAsync(user.Id))
            .ReturnsAsync((IReadOnlyList<UserIdentityModel>)[identity]);
        harness.Users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await Assert.ThrowsAsync<LastCredentialException>(() =>
            harness.Build().UnlinkAsync(user.Id, ExternalAuthProviderType.Yandex)
        );

        harness.Identities.Verify(r => r.Delete(It.IsAny<UserIdentityModel>()), Times.Never);
    }

    [Fact]
    public async Task Unlink_Reports_A_Provider_That_Was_Never_Linked()
    {
        var harness = new Harness();

        await Assert.ThrowsAsync<ExternalIdentityNotLinkedException>(() =>
            harness.Build().UnlinkAsync(Ulid.NewUlid(), ExternalAuthProviderType.Yandex)
        );
    }

    [Fact]
    public void GetProviders_Lists_Only_Enabled_Providers()
    {
        var harness = new Harness();

        harness.Provider.SetupGet(p => p.DisplayName).Returns("Яндекс ID");
        harness.Registry.SetupGet(r => r.Enabled).Returns([harness.Provider.Object]);

        var provider = Assert.Single(harness.Build().GetProviders());

        Assert.Equal(ExternalAuthProviderType.Yandex, provider.Provider);
        Assert.Equal("Яндекс ID", provider.DisplayName);
    }
}
