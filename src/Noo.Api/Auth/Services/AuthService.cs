using Noo.Api.Auth.DTO;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Services;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Users.Types;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(IAuthService))]
public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;

    private readonly IRefreshTokenService _refreshTokenService;

    private readonly IUserService _userService;

    private readonly ISessionService _sessionService;

    private readonly IAuthEmailService _emailService;

    private readonly IAuthUrlGenerator _urlGenerator;

    private readonly IEmailChangeService _emailChangeService;

    private readonly IHashService _hashService;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IAuthEmailService emailService,
        IAuthUrlGenerator urlGenerator,
        IEmailChangeService emailChangeService,
        IUserService userService,
        IHashService hashService,
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _userService = userService;
        _emailService = emailService;
        _hashService = hashService;
        _urlGenerator = urlGenerator;
        _emailChangeService = emailChangeService;
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthTokensResult> LoginAsync(LoginDTO request)
    {
        var user = await _userService.GetUserByUsernameOrEmailAsync(request.UsernameOrEmail);

        if (user == null)
        {
            throw new UnauthorizedException();
        }

        if (!_hashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException();
        }

        if (!user.IsVerified)
        {
            throw new UserIsNotVerifiedException();
        }

        if (user.IsBlocked)
        {
            throw new UserIsBlockedException();
        }

        var context = _httpContextAccessor.HttpContext;

        if (context == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        var sessionId = await _sessionService.CreateSessionIfNotExistsAsync(context, user.Id);
        var response = IssueAccessToken(user, sessionId);
        var (refreshToken, refreshExpiresAt) = _refreshTokenService.IssueRefreshToken(sessionId);

        return new AuthTokensResult(response, refreshToken, refreshExpiresAt);
    }

    public async Task<RefreshResult> RefreshAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrEmpty(rawRefreshToken))
        {
            return RefreshResult.Failed;
        }

        var outcome = await _refreshTokenService.RotateAsync(rawRefreshToken);

        // Replay of an already-rotated token: revoke the whole session, which
        // cascade-deletes its refresh tokens and invalidates outstanding access tokens.
        if (outcome.Status == RefreshOutcomeStatus.Reused)
        {
            if (outcome.Token is not null)
            {
                _sessionService.DeleteSessionById(outcome.Token.SessionId);
            }

            return RefreshResult.Failed;
        }

        if (outcome.Status != RefreshOutcomeStatus.Valid || outcome.Token is null)
        {
            return RefreshResult.Failed;
        }

        // A valid (non cascade-deleted) token implies the session still exists.
        var sessionId = outcome.Token.SessionId;
        var user = await _userService.GetUserByIdAsync(outcome.Token.Session.UserId);

        if (user is null || user.IsBlocked)
        {
            _sessionService.DeleteSessionById(sessionId);
            return RefreshResult.Failed;
        }

        var response = IssueAccessToken(user, sessionId);
        var (refreshToken, refreshExpiresAt) = _refreshTokenService.IssueRefreshToken(sessionId);

        return new RefreshResult(response, refreshToken, refreshExpiresAt);
    }

    private LoginResponseDTO IssueAccessToken(UserModel user, Ulid sessionId)
    {
        var (token, expiresAt) = _tokenService.GenerateAccessToken(
            new AccessTokenPayload()
            {
                UserId = user.Id,
                UserRole = user.Role,
                SessionId = sessionId,
            }
        );

        return new LoginResponseDTO
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            UserRole = user.Role,
        };
    }

    public async Task RegisterAsync(RegisterDTO request)
    {
        var exists = await _userService.UserExistsAsync(request.Username, request.Email);

        if (exists)
        {
            throw new AlreadyExistsException();
        }

        var passwordHash = _hashService.Hash(request.Password);

        var userId = _userService.CreateUser(
            new UserCreationPayload
            {
                Username = request.Username,
                Email = request.Email,
                Name = request.Name,
                PasswordHash = passwordHash,
                Role = UserRoles.Student,
            }
        );

        var verificationToken = _tokenService.CreateToken(userId, TokenType.EmailVerification);
        var verificationLink = _urlGenerator.GenerateEmailVerificationUrl(verificationToken.Token);

        await _emailService.SendEmailVerificationEmailAsync(
            request.Email,
            request.Name,
            verificationLink
        );
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _userService.GetUserByUsernameOrEmailAsync(email);

        if (user == null)
        {
            throw new NotFoundException();
        }

        var token = _tokenService.CreateToken(user.Id, TokenType.PasswordReset);
        var link = _urlGenerator.GeneratePasswordResetUrl(token.Token);

        await _emailService.SendForgotPasswordEmailAsync(email, user.Name, link);
    }

    public async Task ConfirmPasswordResetAsync(string token, string newPassword)
    {
        var (userId, tokenType, _) = await _tokenService.ValidateTokenAsync(token);

        if (userId is null || tokenType != TokenType.PasswordReset)
        {
            throw new UnauthorizedException();
        }

        var user = await _userService.GetUserByIdAsync((Ulid)userId);

        if (user == null)
        {
            throw new NotFoundException();
        }

        await _userService.UpdateUserPasswordAsync(user.Id, _hashService.Hash(newPassword));
        _sessionService.DeleteAllSessions(user.Id);
        _tokenService.DeleteTokens(user.Id, TokenType.PasswordReset);
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var (userId, tokenType, newEmail) = await _tokenService.ValidateTokenAsync(token);

        if (userId == null || tokenType == null)
        {
            throw new UnauthorizedException();
        }

        if (tokenType == TokenType.EmailVerification && string.IsNullOrEmpty(newEmail))
        {
            var user =
                await _userService.GetUserByIdAsync(userId.Value)
                ?? throw new UnauthorizedException();

            user.IsVerified = true;
            _tokenService.DeleteTokens(user.Id, TokenType.EmailVerification);
            return;
        }

        if (tokenType == TokenType.EmailChange && !string.IsNullOrEmpty(newEmail))
        {
            await _emailChangeService.ConfirmAsync(userId.Value, newEmail);
            return;
        }

        throw new UnauthorizedException();
    }

    public async Task<bool> IsUsernameFreeAsync(string username)
    {
        var usernameIsTaken = await _userService.UserExistsAsync(username, null);
        return !usernameIsTaken;
    }
}
