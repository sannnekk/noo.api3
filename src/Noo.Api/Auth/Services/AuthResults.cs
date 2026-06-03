using Noo.Api.Auth.DTO;

namespace Noo.Api.Auth.Services;

/// <summary>
/// Result of a login: the response body plus the raw refresh token that the
/// controller writes into the httpOnly cookie.
/// </summary>
public record AuthTokensResult(
    LoginResponseDTO Response,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);

/// <summary>
/// Result of a refresh attempt. On failure, <see cref="Response"/> is null and the
/// controller responds 401 and clears the cookie.
/// </summary>
public record RefreshResult(
    LoginResponseDTO? Response,
    string? RefreshToken,
    DateTime RefreshTokenExpiresAt
)
{
    public bool Succeeded => Response is not null && RefreshToken is not null;

    public static readonly RefreshResult Failed = new(null, null, default);
}
