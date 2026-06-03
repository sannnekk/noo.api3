using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Security;

[RegisterTransient(typeof(IJwtService))]
public class JwtService : IJwtService
{
    private JwtConfig _config;

    public JwtService(IOptions<JwtConfig> config)
    {
        _config = config.Value;
    }

    public (string, DateTime) GenerateToken(IEnumerable<Claim> claims)
    {
        var creds = new SigningCredentials(
            _config.SymmetricSecurityKey,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_config.AccessTokenExpireTimeSpan),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
    }
}
