using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace Noo.Api.Core.Config.Env;

public class JwtConfig : IConfig
{
    public static string SectionName => "Jwt";

    [Required]
    public required string Secret { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    /// <summary>
    /// Lifetime of a refresh token, in days.
    /// </summary>
    [Required]
    public required int ExpireDays { get; init; }

    /// <summary>
    /// Lifetime of an access token (JWT), in minutes. Kept short so that a
    /// revoked session is reflected quickly; renewed via the refresh token.
    /// </summary>
    public int AccessTokenExpireMinutes { get; init; } = 15;

    public TimeSpan AccessTokenExpireTimeSpan => TimeSpan.FromMinutes(AccessTokenExpireMinutes);

    public TimeSpan RefreshTokenExpireTimeSpan => TimeSpan.FromDays(ExpireDays);

    public SymmetricSecurityKey SymmetricSecurityKey =>
        new SymmetricSecurityKey(Convert.FromBase64String(Secret));
}
