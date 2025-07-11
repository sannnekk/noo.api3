using Noo.Api.Auth.DTO;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Types;
using Noo.Api.Users.Services;
using Microsoft.Extensions.Options;
using Noo.Api.Sessions.Services;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(IAuthService))]
public class AuthService : IAuthService
{
    private readonly IAuthTokenService _tokenService;

    private readonly IUserService _userService;

    private readonly ISessionService _sessionService;

    private readonly IAuthEmailService _emailService;

    private readonly IAuthUrlGenerator _urlGenerator;

    private readonly IHashService _hashService;

    private readonly JwtConfig _jwtConfig;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IAuthTokenService tokenService,
        IAuthEmailService emailService,
        IAuthUrlGenerator urlGenerator,
        IUserService userService,
        IHashService hashService,
        IOptions<JwtConfig> jwtConfig,
        ISessionService sessionService,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _tokenService = tokenService;
        _userService = userService;
        _emailService = emailService;
        _hashService = hashService;
        _jwtConfig = jwtConfig.Value;
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

        var sessionId = await _sessionService.CreateSessionIfNotExistsAsync(
            context,
            user.Id
        );

        var ExpiresAt = DateTime.UtcNow.AddDays(_jwtConfig.ExpireDays);

        var token = _tokenService.GenerateAccessToken(new AccessTokenPayload()
        {
            UserId = user.Id,
            UserRole = user.Role,
            SessionId = sessionId,
            ExpiresAt = ExpiresAt
        });

        return new LoginResponseDTO
        {
            AccessToken = token,
            ExpiresAt = ExpiresAt,
            UserInfo = new UserInfoDTO
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Username = user.Username,
                Role = user.Role
            }
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

        var userId = await _userService.CreateUserAsync(new UserCreationPayload
        {
            Username = request.Username,
            Email = request.Email,
            Name = request.Name,
            PasswordHash = passwordHash,
            Role = UserRoles.Student
        });

        var verificationToken = _tokenService.GenerateEmailVerificationToken(userId);
        var verificationLink = _urlGenerator.GenerateEmailVerificationUrl(verificationToken);

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

        var token = _tokenService.GeneratePasswordResetToken(user.Id);
        var link = _urlGenerator.GeneratePasswordResetUrl(token);

        await _emailService.SendForgotPasswordEmailAsync(email, user.Name, link);
    }

    public async Task ConfirmPasswordResetAsync(string token, string newPassword)
    {
        var userId = _tokenService.ValidatePasswordResetToken(token);

        if (userId is null)
        {
            throw new UnauthorizedException();
        }

        var user = await _userService.GetUserByIdAsync((Ulid)userId);

        if (user == null)
        {
            throw new NotFoundException();
        }

        await _userService.UpdateUserPasswordAsync(user.Id, _hashService.Hash(newPassword));
        await _sessionService.DeleteAllSessionsAsync(user.Id);
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

        var token = _tokenService.GenerateEmailChangeToken(user.Id, newEmail);
        var link = _urlGenerator.GenerateEmailChangeUrl(token);

        await _emailService.SendEmailChangeEmailAsync(newEmail, user.Name, link);
    }

    public async Task ConfirmEmailChangeAsync(Ulid userId, string token)
    {
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new UnauthorizedException();
        }

        var newEmail = _tokenService.ValidateEmailChangeToken(token);

        if (newEmail == null)
        {
            throw new UnauthorizedException();
        }

        await _userService.UpdateUserEmailAsync(user.Id, newEmail);
    }

    public async Task<bool> CheckUsernameAsync(string username)
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
