using Noo.Api.Auth.Models;

namespace Noo.Api.Auth.Services;

public interface ITokenService
{
    public (string, DateTime) GenerateAccessToken(AccessTokenPayload payload);

    public TokenModel CreateToken(Ulid userId, TokenType type, string? payload = null);

    /// <summary>
    /// Validates the token and returns the user ID if valid, otherwise null.
    /// </summary>
    public Task<(Ulid?, TokenType?, string?)> ValidateTokenAsync(string token);
    public void DeleteTokens(Ulid id, TokenType tokenType);
}
