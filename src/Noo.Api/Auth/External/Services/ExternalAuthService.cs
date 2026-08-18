using Noo.Api.Auth.External.DTO;
using Noo.Api.Auth.External.Exceptions;
using Noo.Api.Auth.External.Models;
using Noo.Api.Auth.External.Providers;
using Noo.Api.Auth.External.Types;
using Noo.Api.Auth.Services;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Users.Types;

namespace Noo.Api.Auth.External.Services;

[RegisterScoped(typeof(IExternalAuthService))]
public class ExternalAuthService : IExternalAuthService
{
    /// <summary>
    /// Roles whose accounts may be claimed by a matching provider email. Privileged roles are
    /// deliberately absent: a takeover there is worth far more than the saved click.
    /// </summary>
    private static readonly UserRoles[] _autoLinkableRoles = [UserRoles.Student, UserRoles.Mentor];

    private const int _maxAvatarUrlLength = 255;

    private readonly IExternalAuthProviderRegistry _registry;

    private readonly IExternalAuthChallengeStore _challenges;

    private readonly IUserIdentityRepository _identities;

    private readonly IUserRepository _users;

    private readonly IUserAvatarRepository _avatars;

    private readonly IUserService _userService;

    private readonly IUsernameGenerator _usernames;

    private readonly IAuthService _authService;

    private readonly IAuthUrlGenerator _urlGenerator;

    public ExternalAuthService(
        IExternalAuthProviderRegistry registry,
        IExternalAuthChallengeStore challenges,
        IUserIdentityRepository identities,
        IUserRepository users,
        IUserAvatarRepository avatars,
        IUserService userService,
        IUsernameGenerator usernames,
        IAuthService authService,
        IAuthUrlGenerator urlGenerator
    )
    {
        _registry = registry;
        _challenges = challenges;
        _identities = identities;
        _users = users;
        _avatars = avatars;
        _userService = userService;
        _usernames = usernames;
        _authService = authService;
        _urlGenerator = urlGenerator;
    }

    public IReadOnlyList<ExternalAuthProviderDTO> GetProviders()
    {
        return _registry
            .Enabled.Select(provider => new ExternalAuthProviderDTO
            {
                Provider = provider.Type,
                DisplayName = provider.DisplayName,
            })
            .ToList();
    }

    public async Task<string> StartAsync(
        ExternalAuthProviderType provider,
        ExternalAuthIntent intent,
        string? returnUrl,
        Ulid? userId
    )
    {
        var authProvider = _registry.Get(provider);

        var challenge = new ExternalAuthChallenge
        {
            Provider = provider,
            Intent = intent,
            State = RandomGenerator.GenerateRandomUrlToken(),
            CodeVerifier = Pkce.CreateVerifier(),
            RedirectUri = _urlGenerator.GenerateExternalAuthCallbackUrl(provider),
            ReturnUrl = SanitizeReturnUrl(returnUrl),
            UserId = userId,
        };

        await _challenges.SaveAsync(challenge);

        return authProvider.BuildAuthorizationUrl(
            challenge,
            Pkce.CreateChallenge(challenge.CodeVerifier)
        );
    }

    public async Task<ExternalAuthOutcome> CompleteAsync(
        ExternalAuthProviderType provider,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default
    )
    {
        var callback = new ExternalAuthCallback(parameters);
        var challenge = await _challenges.RedeemAsync(callback.Require("state"));

        if (challenge is null || challenge.Provider != provider)
        {
            throw new ExternalAuthStateInvalidException();
        }

        var authProvider = _registry.Get(provider);
        var profile = await authProvider.ResolveProfileAsync(callback, challenge, ct);
        var identity = await _identities.GetByProviderAndSubjectAsync(provider, profile.SubjectId);

        if (challenge.Intent == ExternalAuthIntent.Link)
        {
            LinkIdentity(challenge, profile, identity);

            return new ExternalAuthOutcome(challenge.Intent, provider, challenge.ReturnUrl, null);
        }

        UserModel user;

        if (identity is not null)
        {
            Touch(identity, profile);
            user = identity.User;
        }
        else
        {
            user = await ResolveUserAsync(authProvider, profile);
        }

        await ApplyAvatarAsync(user.Id, profile);

        // No IsVerified check: a provider grant is verification. IssueSessionAsync still
        // rejects blocked accounts.
        var session = await _authService.IssueSessionAsync(user);

        return new ExternalAuthOutcome(challenge.Intent, provider, challenge.ReturnUrl, session);
    }

    public Task<IReadOnlyList<UserIdentityModel>> GetIdentitiesAsync(Ulid userId)
    {
        return _identities.GetByUserAsync(userId);
    }

    public async Task UnlinkAsync(Ulid userId, ExternalAuthProviderType provider)
    {
        var identities = await _identities.GetByUserAsync(userId);

        var identity =
            identities.FirstOrDefault(candidate => candidate.Provider == provider)
            ?? throw new ExternalIdentityNotLinkedException();

        var user = await _users.GetByIdAsync(userId) ?? throw new NotFoundException();

        // Unlinking the only way in would lock the account out for good.
        if (user.PasswordHash is null && identities.Count <= 1)
        {
            throw new LastCredentialException();
        }

        _identities.Delete(identity);
    }

    private void LinkIdentity(
        ExternalAuthChallenge challenge,
        ExternalUserProfile profile,
        UserIdentityModel? identity
    )
    {
        var userId = challenge.UserId ?? throw new ExternalAuthStateInvalidException();

        if (identity is not null)
        {
            if (identity.UserId != userId)
            {
                throw new ExternalIdentityAlreadyLinkedException(
                    "Этот аккаунт уже привязан к другому профилю."
                );
            }

            Touch(identity, profile);
            return;
        }

        AddIdentity(userId, challenge.Provider, profile);
    }

    private async Task<UserModel> ResolveUserAsync(
        IExternalAuthProvider provider,
        ExternalUserProfile profile
    )
    {
        var existing = profile.Email is null ? null : await _users.GetByEmailAsync(profile.Email);

        if (existing is not null)
        {
            var canAutoLink =
                profile.EmailIsVerified
                && provider.EmailIsTrusted
                && _autoLinkableRoles.Contains(existing.Role);

            // Creating a second account would only fail on the unique email index, so say
            // plainly what to do instead.
            if (!canAutoLink)
            {
                throw new ExternalAuthEmailConflictException();
            }

            AddIdentity(existing.Id, provider.Type, profile);

            return existing;
        }

        var user = await _userService.CreateUserAsync(
            new UserCreationPayload
            {
                Username = await _usernames.GenerateAsync(profile, provider.Type),
                Email = profile.Email,
                Name = BuildName(profile),
                Role = UserRoles.Student,
            }
        );

        user.IsVerified = true;

        AddIdentity(user.Id, provider.Type, profile);

        return user;
    }

    private void AddIdentity(
        Ulid userId,
        ExternalAuthProviderType provider,
        ExternalUserProfile profile
    )
    {
        _identities.Add(
            new UserIdentityModel
            {
                UserId = userId,
                Provider = provider,
                SubjectId = profile.SubjectId,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                LastLoginAt = Clock.Now,
            }
        );
    }

    private static void Touch(UserIdentityModel identity, ExternalUserProfile profile)
    {
        identity.Email = profile.Email;
        identity.DisplayName = profile.DisplayName;
        identity.LastLoginAt = Clock.Now;
    }

    private async Task ApplyAvatarAsync(Ulid userId, ExternalUserProfile profile)
    {
        if (profile.AvatarUrl is not { Length: > 0 and <= _maxAvatarUrlLength } avatarUrl)
        {
            return;
        }

        var avatar = await _avatars.GetUserAvatarByUserIdAsync(userId);

        if (avatar is null)
        {
            avatar = new UserAvatarModel { UserId = userId };
            _avatars.Add(avatar);
        }
        // An uploaded picture is the user's own choice and outranks the provider's.
        else if (avatar.AvatarType is not (UserAvatarType.None or UserAvatarType.External))
        {
            return;
        }

        avatar.AvatarType = UserAvatarType.External;
        avatar.AvatarUrl = avatarUrl;
    }

    private static string BuildName(ExternalUserProfile profile)
    {
        var fullName = $"{profile.FirstName} {profile.LastName}".Trim();

        return Coalesce(profile.DisplayName, fullName, profile.ProviderLogin) ?? "Пользователь";
    }

    private static string? Coalesce(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    /// <summary>
    /// The only place a request can influence where a browser ends up, so an absolute URL
    /// or a protocol-relative path is rejected outright rather than silently dropped.
    /// </summary>
    private static string? SanitizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var isRelativePath =
            returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.Contains('\\', StringComparison.Ordinal);

        return isRelativePath
            ? returnUrl
            : throw new BadRequestException("Адрес возврата должен быть относительным путём.");
    }
}
