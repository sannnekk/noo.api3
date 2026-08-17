using Moq;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Services;
using Noo.Api.Auth.External.Types;

namespace Noo.UnitTests.Auth.External;

public class ExternalAuthProviderRegistryTests
{
    private static IExternalAuthProvider Provider(ExternalAuthProviderType type, bool isEnabled)
    {
        var provider = new Mock<IExternalAuthProvider>();

        provider.SetupGet(p => p.Type).Returns(type);
        provider.SetupGet(p => p.IsEnabled).Returns(isEnabled);

        return provider.Object;
    }

    [Fact]
    public void Get_Resolves_By_Type()
    {
        var yandex = Provider(ExternalAuthProviderType.Yandex, true);
        var registry = new ExternalAuthProviderRegistry(
            [yandex, Provider(ExternalAuthProviderType.Vk, true)]
        );

        Assert.Same(yandex, registry.Get(ExternalAuthProviderType.Yandex));
    }

    [Fact]
    public void Get_Throws_For_An_Unregistered_Provider()
    {
        var registry = new ExternalAuthProviderRegistry([Provider(ExternalAuthProviderType.Yandex, true)]);

        Assert.Throws<UnknownExternalAuthProviderException>(
            () => registry.Get(ExternalAuthProviderType.Vk)
        );
    }

    [Fact]
    public void Get_Throws_For_A_Disabled_Provider()
    {
        var registry = new ExternalAuthProviderRegistry([Provider(ExternalAuthProviderType.Vk, false)]);

        Assert.Throws<UnknownExternalAuthProviderException>(
            () => registry.Get(ExternalAuthProviderType.Vk)
        );
    }

    [Fact]
    public void Enabled_Hides_Unconfigured_Providers()
    {
        var registry = new ExternalAuthProviderRegistry(
            [Provider(ExternalAuthProviderType.Yandex, true), Provider(ExternalAuthProviderType.Vk, false)]
        );

        Assert.Equal(
            [ExternalAuthProviderType.Yandex],
            registry.Enabled.Select(provider => provider.Type)
        );
    }
}
