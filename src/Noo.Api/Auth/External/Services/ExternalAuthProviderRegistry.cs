using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Types;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.External.Services;

[RegisterScoped(typeof(IExternalAuthProviderRegistry))]
public class ExternalAuthProviderRegistry : IExternalAuthProviderRegistry
{
    private readonly Dictionary<ExternalAuthProviderType, IExternalAuthProvider> _providers;

    public ExternalAuthProviderRegistry(IEnumerable<IExternalAuthProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.Type);
    }

    public IEnumerable<IExternalAuthProvider> Enabled =>
        _providers.Values.Where(provider => provider.IsEnabled);

    public IExternalAuthProvider Get(ExternalAuthProviderType type)
    {
        return _providers.TryGetValue(type, out var provider) && provider.IsEnabled
            ? provider
            : throw new UnknownExternalAuthProviderException(type);
    }
}
