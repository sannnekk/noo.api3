using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.Services;

public interface IExternalAuthProviderRegistry
{
    /// <summary>Throws UnknownExternalAuthProviderException when the provider is absent or disabled.</summary>
    public IExternalAuthProvider Get(ExternalAuthProviderType type);

    public IEnumerable<IExternalAuthProvider> Enabled { get; }
}
