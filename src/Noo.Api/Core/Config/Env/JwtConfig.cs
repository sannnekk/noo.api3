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

    [Required]
    public required int ExpireDays { get; init; }

    public TimeSpan ExpireTimeSpan => TimeSpan.FromDays(ExpireDays);

    public SymmetricSecurityKey SymmetricSecurityKey =>
        new SymmetricSecurityKey(Convert.FromBase64String(Secret));
}
