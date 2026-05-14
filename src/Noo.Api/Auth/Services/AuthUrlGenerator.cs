using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.Services;

[RegisterTransient(typeof(IAuthUrlGenerator))]
public class AuthUrlGenerator : IAuthUrlGenerator
{
    private readonly AppConfig _appConfig;

    public AuthUrlGenerator(IOptions<AppConfig> appConfig)
    {
        _appConfig = appConfig.Value;
    }

    public string GenerateEmailVerificationUrl(string token)
    {
        return $"{_appConfig.BaseUrl}/auth/verify-email?token={token}";
    }

    public string GeneratePasswordResetUrl(string token)
    {
        return $"{_appConfig.BaseUrl}/auth/reset-password?token={token}";
    }

    public string GenerateEmailChangeUrl(string token)
    {
        return $"{_appConfig.BaseUrl}/auth/verify-email?token={token}";
    }
}
