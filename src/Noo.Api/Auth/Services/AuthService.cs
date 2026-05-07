using Noo.Api.Auth.DTO;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Sessions.Services;
using Noo.Api.Users.Services;
using Noo.Api.Users.Types;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(IAuthService))]
public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;

    private readonly IUserService _userService;

    private readonly ISessionService _sessionService;

    private readonly IAuthEmailService _emailService;

    private readonly IAuthUrlGenerator _urlGenerator;

    private readonly IHashService _hashService;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        ITokenService tokenService,
        IAuthEmailService emailService,
        IAuthUrlGenerator urlGenerator,
        IUserService userService,
        IHashService hashService,
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _tokenService = tokenService;
        _userService = userService;
        _emailService = emailService;
        _hashService = hashService;
        _urlGenerator = urlGenerator;
        _sessionService = sessionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginDTO request)
    {
        var user = await _userService.GetUserByUsernameOrEmailAsync(request.UsernameOrEmail);

        if (user == null)
        {
            throw new UnauthorizedException();
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
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
            UserInfo = new UserInfoDTO
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Username = user.Username,
                Role = user.Role,
            },
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

    public async Task RequestEmailChangeAsync(Ulid userId, string newEmail)
    {
        var exists = await _userService.UserExistsAsync(null, newEmail);

        if (exists)
        {
            throw new AlreadyExistsException();
        }

        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new UnauthorizedException();
        }

        var token = _tokenService.CreateToken(user.Id, TokenType.EmailChange);
        var link = _urlGenerator.GenerateEmailChangeUrl(token.Token);

        await _emailService.SendEmailChangeEmailAsync(newEmail, user.Name, link);
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var (userId, tokenType, newEmail) = await _tokenService.ValidateTokenAsync(token);

        if (userId == null || tokenType == null)
        {
            throw new UnauthorizedException();
        }

        var user = await _userService.GetUserByIdAsync(userId.Value);

        if (user == null)
        {
            throw new UnauthorizedException();
        }

        if (string.IsNullOrEmpty(newEmail) && tokenType == TokenType.EmailVerification)
        {
            // In case of just verification of the email
            user.IsVerified = true;
            _tokenService.DeleteTokens(user.Id, TokenType.EmailVerification);
            return;
        }

        // in case of email change
        if (tokenType != TokenType.EmailChange || string.IsNullOrEmpty(newEmail))
        {
            throw new UnauthorizedException();
        }

        await _userService.UpdateUserEmailAsync(user.Id, newEmail);
        _tokenService.DeleteTokens(user.Id, TokenType.EmailChange);
    }

    public async Task<bool> IsUsernameFreeAsync(string username)
    {
        var usernameIsTaken = await _userService.UserExistsAsync(username, null);
        return !usernameIsTaken;
    }

    private bool VerifyPassword(string passwordToCheck, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordToCheck) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        return passwordHash == _hashService.Hash(passwordToCheck);
    }
}
