using System.Text.RegularExpressions;
using Moq;
using Noo.Api.Auth.External.Services;
using Noo.Api.Auth.External.Types;
using Noo.Api.Users.Services;

namespace Noo.UnitTests.Auth.External;

public class UsernameGeneratorTests
{
    /// <summary>The frontend's own username rule, which generated names must satisfy.</summary>
    private static readonly Regex _frontendRule = new("^[a-zA-Z0-9_-]{3,20}$");

    private static (UsernameGenerator Generator, Mock<IUserRepository> Users) Build(
        params string[] taken
    )
    {
        var users = new Mock<IUserRepository>();

        users
            .Setup(r => r.ExistsByUsernameOrEmailAsync(It.IsAny<string>(), null))
            .ReturnsAsync((string? username, string? _) => taken.Contains(username));

        return (new UsernameGenerator(users.Object), users);
    }

    private static ExternalUserProfile Profile(
        string? login = null,
        string? firstName = null,
        string? lastName = null,
        string? displayName = null,
        string? email = null
    ) =>
        new()
        {
            SubjectId = "subject",
            ProviderLogin = login,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Email = email,
        };

    [Fact]
    public async Task Prefers_The_Provider_Login()
    {
        var (generator, _) = Build();

        var username = await generator.GenerateAsync(
            Profile(login: "ivanpetrov", firstName: "Иван", email: "other@example.com"),
            ExternalAuthProviderType.Yandex
        );

        Assert.Equal("ivanpetrov", username);
    }

    [Fact]
    public async Task Transliterates_A_Cyrillic_Name()
    {
        var (generator, _) = Build();

        var username = await generator.GenerateAsync(
            Profile(firstName: "Иван", lastName: "Петров"),
            ExternalAuthProviderType.Vk
        );

        Assert.Equal("ivan-petrov", username);
    }

    [Fact]
    public async Task Falls_Back_Through_Display_Name_Then_Email_Then_Provider()
    {
        var (generator, _) = Build();

        Assert.Equal(
            "jane-doe",
            await generator.GenerateAsync(
                Profile(displayName: "Jane Doe", email: "jd@example.com"),
                ExternalAuthProviderType.Yandex
            )
        );
        Assert.Equal(
            "jdmailbox",
            await generator.GenerateAsync(
                Profile(email: "jd.mailbox@example.com"),
                ExternalAuthProviderType.Yandex
            )
        );
        Assert.Equal(
            "yandex",
            await generator.GenerateAsync(Profile(), ExternalAuthProviderType.Yandex)
        );
        // "vk" is below the three-character minimum, so even the provider key falls through.
        Assert.Equal(
            "user",
            await generator.GenerateAsync(Profile(), ExternalAuthProviderType.Vk)
        );
    }

    [Fact]
    public async Task Appends_A_Digit_When_The_Seed_Is_Taken()
    {
        var (generator, _) = Build("ivan", "ivan2", "ivan3");

        var username = await generator.GenerateAsync(
            Profile(login: "ivan"),
            ExternalAuthProviderType.Yandex
        );

        Assert.Equal("ivan4", username);
    }

    [Fact]
    public async Task Falls_Back_To_A_Random_Suffix_When_Every_Digit_Is_Taken()
    {
        var (generator, _) = Build("ivan", "ivan2", "ivan3", "ivan4", "ivan5", "ivan6", "ivan7", "ivan8", "ivan9");

        var username = await generator.GenerateAsync(
            Profile(login: "ivan"),
            ExternalAuthProviderType.Yandex
        );

        Assert.StartsWith("ivan-", username);
        Assert.Equal("ivan-".Length + 4, username.Length);
    }

    [Theory]
    [InlineData("a-very-long-provider-login-that-will-not-fit")]
    [InlineData("Ярослав-Константинопольский")]
    [InlineData("!!!")]
    [InlineData("测试")]
    public async Task Always_Produces_A_Name_The_Frontend_Validator_Accepts(string login)
    {
        var (generator, _) = Build();

        var username = await generator.GenerateAsync(
            Profile(login: login),
            ExternalAuthProviderType.Yandex
        );

        Assert.Matches(_frontendRule, username);
    }

    [Fact]
    public async Task Keeps_Random_Candidates_Within_The_Length_Limit()
    {
        const string seed = "a very long provider login";
        const string clamped = "a-very-long-pro";
        var (generator, _) = Build(
            clamped,
            $"{clamped}2",
            $"{clamped}3",
            $"{clamped}4",
            $"{clamped}5",
            $"{clamped}6",
            $"{clamped}7",
            $"{clamped}8",
            $"{clamped}9"
        );

        var username = await generator.GenerateAsync(
            Profile(login: seed),
            ExternalAuthProviderType.Yandex
        );

        Assert.Matches(_frontendRule, username);
        Assert.StartsWith($"{clamped}-", username);
    }
}
