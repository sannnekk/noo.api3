using Microsoft.Extensions.Options;
using Noo.Api.Auth.Models;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Security;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(IRefreshTokenService))]
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    private readonly IHashService _hashService;

    private readonly JwtConfig _jwtConfig;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        IHashService hashService,
        IOptions<JwtConfig> jwtConfig
    )
    {
        _refreshTokenRepository = refreshTokenRepository;
        _hashService = hashService;
        _jwtConfig = jwtConfig.Value;
    }

    public (string Token, DateTime ExpiresAt) IssueRefreshToken(Ulid sessionId)
    {
        var rawToken = RandomGenerator.GenerateRandomUrlToken();
        var expiresAt = Clock.Now.Add(_jwtConfig.RefreshTokenExpireTimeSpan);

        _refreshTokenRepository.Add(
            new RefreshTokenModel
            {
                SessionId = sessionId,
                TokenHash = _hashService.Hash(rawToken),
                ExpiresAt = expiresAt,
            }
        );

        return (rawToken, expiresAt);
    }

    public async Task<RefreshOutcome> RotateAsync(string rawToken)
    {
        var token = await _refreshTokenRepository.GetByHashAsync(_hashService.Hash(rawToken));

        if (token is null || token.ExpiresAt < Clock.Now)
        {
            return new RefreshOutcome(RefreshOutcomeStatus.Invalid);
        }

        if (token.UsedAt is not null)
        {
            return new RefreshOutcome(RefreshOutcomeStatus.Reused, token);
        }

        token.UsedAt = Clock.Now;
        token.UpdatedAt = Clock.Now;

        return new RefreshOutcome(RefreshOutcomeStatus.Valid, token);
    }
}
