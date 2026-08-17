using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.External.Providers;

/// <summary>
/// One external identity provider. Register an implementation with
/// <c>[RegisterScoped(typeof(IExternalAuthProvider))]</c> and it is discovered automatically —
/// adding a provider touches no existing file.
/// </summary>
public interface IExternalAuthProvider
{
    public ExternalAuthProviderType Type { get; }

    public string DisplayName { get; }

    /// <summary>False when the provider has no credentials configured in this environment.</summary>
    public bool IsEnabled { get; }

    /// <summary>Whether an email this provider reports is good enough to auto-link an account.</summary>
    public bool EmailIsTrusted { get; }

    public string BuildAuthorizationUrl(ExternalAuthChallenge challenge, string codeChallenge);

    /// <summary>Exchanges the callback for a profile. One call, so no provider token escapes.</summary>
    public Task<ExternalUserProfile> ResolveProfileAsync(
        ExternalAuthCallback callback,
        ExternalAuthChallenge challenge,
        CancellationToken ct = default
    );
}
