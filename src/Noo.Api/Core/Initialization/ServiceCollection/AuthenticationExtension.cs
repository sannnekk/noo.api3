using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Initialization.Configuration;
using Noo.Api.Core.Security.Authentication;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Media;
using Noo.Api.Sessions.Services;

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
                {
                    options.TokenValidationParameters = GetTokenValidationParameters(jwtConfig);
                    options.Events = new JwtBearerEvents
                    {
                        // A token is only valid while its backing session still exists.
                        // Failing here yields 401 on auth-required routes; [AllowAnonymous]
                        // routes are unaffected since failed auth does not block them.
                        OnTokenValidated = ValidateSessionExistsAsync,
                    };
                }
            )
            // Same JWT validation, but the token is read from the httpOnly media cookie
            // instead of the Authorization header, so that plain <img> requests to
            // /media/{id}/raw can authenticate. Opt-in per route via
            // [Authorize(AuthenticationSchemes = MediaCookie.Scheme)].
            .AddJwtBearer(
                MediaCookie.Scheme,
                options =>
                {
                    options.TokenValidationParameters = GetTokenValidationParameters(jwtConfig);
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ReadTokenFromMediaCookieAsync,
                        OnTokenValidated = ValidateSessionExistsAsync,
                    };
                }
            );
    }

    private static Task ReadTokenFromMediaCookieAsync(MessageReceivedContext context)
    {
        if (context.Request.Cookies.TryGetValue(MediaCookie.Name, out var token))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    }

    private static async Task ValidateSessionExistsAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (principal is null)
        {
            context.Fail("Token has no associated identity.");
            return;
        }

        var sessionId = principal.GetSessionId();
        var userId = principal.GetId();

        if (sessionId == Ulid.Empty || userId == Ulid.Empty)
        {
            context.Fail("Token is not associated with a session.");
            return;
        }

        var sessionService =
            context.HttpContext.RequestServices.GetRequiredService<ISessionService>();

        if (!await sessionService.SessionExistsAsync(sessionId, userId))
        {
            context.Fail("Session no longer exists.");
        }
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
