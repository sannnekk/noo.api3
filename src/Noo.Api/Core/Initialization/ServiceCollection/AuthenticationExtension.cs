using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.Security.Authentication;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class AuthenticationExtension
{
    public static void AddNooAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var appConfig = configuration.GetSection(AppConfig.SectionName).GetOrThrow<AppConfig>();

        switch (appConfig.AuthenticationType)
        {
            case AuthenticationType.Bearer:
                AddBearerAuthentication(services, configuration);
                break;
            default:
                throw new NotSupportedException("Authentication type not supported.");
        }
    }

    private static void AddBearerAuthentication(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtConfig = configuration.GetSection(JwtConfig.SectionName).GetOrThrow<JwtConfig>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                    options.TokenValidationParameters = GetTokenValidationParameters(jwtConfig)
            );
    }

    private static TokenValidationParameters GetTokenValidationParameters(JwtConfig jwtConfig)
    {
        return new TokenValidationParameters
        {
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtConfig.SymmetricSecurityKey,
        };
    }
}
