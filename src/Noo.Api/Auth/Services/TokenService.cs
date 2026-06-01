using System.Security.Claims;
using Noo.Api.Auth.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(ITokenService))]
public class AuthTokenService : ITokenService
{
    private readonly IJwtService _jwtService;

    private readonly ITokenRepository _tokenRepository;

    private readonly IUnitOfWork _unitOfWork;

    public AuthTokenService(
        ITokenRepository tokenRepository,
        IJwtService jwtService,
        IUnitOfWork unitOfWork
    )
    {
        _tokenRepository = tokenRepository;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public TokenModel CreateToken(Ulid userId, TokenType type, string? payload = null)
    {
        var token = new TokenModel
        {
            UserId = userId,
            Type = type,
            Token = RandomGenerator.GenerateRandomUrlToken(),
            ExpiresAt = type switch
            {
                TokenType.PasswordReset => Clock.Now.Add(AuthConfig.ResetPasswordExpireTime),
                TokenType.EmailVerification => Clock.Now.Add(
                    AuthConfig.ConfirmEmailExpireTime
                ),
                TokenType.EmailChange => Clock.Now.Add(AuthConfig.ConfirmEmailExpireTime),
                _ => throw new NotImplementedException($"Token type {type} not implemented"),
            },
        };

        if (string.IsNullOrEmpty(payload) is false)
        {
            token.Payload = payload;
        }

        _tokenRepository.Add(token);

        return token;
    }

    public void DeleteTokens(Ulid id, TokenType passwordReset)
    {
        _tokenRepository.DeleteTokens(id, passwordReset);
    }

    public (string, DateTime) GenerateAccessToken(AccessTokenPayload payload)
    {
        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
            new Claim(ClaimTypes.Sid, payload.SessionId.ToString()),
            new Claim(ClaimTypes.Role, payload.UserRole.ToString()),
        ];

        return _jwtService.GenerateToken(claims);
    }

    public async Task<(Ulid?, TokenType?, string?)> ValidateTokenAsync(string tokenString)
    {
        var token = await _tokenRepository.GetAsync(tokenString);

        return token switch
        {
            null => (null, null, null),
            _ when token.ExpiresAt < Clock.Now => (null, null, null),
            _ => (token.UserId, token.Type, token.Payload),
        };
    }
}
