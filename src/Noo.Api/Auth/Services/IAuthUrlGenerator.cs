using Noo.Api.Auth.External.Types;

namespace Noo.Api.Auth.Services;

public interface IAuthUrlGenerator
{

    public string GenerateEmailVerificationUrl(string token);

    public string GeneratePasswordResetUrl(string token);

    public string GenerateEmailChangeUrl(string token);

    /// <summary>Derived from BaseUrl rather than configured, so it cannot drift per provider.</summary>
    public string GenerateExternalAuthCallbackUrl(ExternalAuthProviderType provider);
}
