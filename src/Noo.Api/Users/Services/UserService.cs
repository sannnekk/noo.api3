using AutoMapper;
using Noo.Api.Auth.Services;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Events;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Specifications;
using Noo.Api.Users.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IUserService))]
public class UserService : IUserService
{
    /// <summary>
    /// How long a block decision is trusted without asking the database. Blocking and
    /// unblocking both drop the key, so this only bounds a change made by some other means.
    /// </summary>
    private static readonly TimeSpan _blockedCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IUserRepository _userRepository;

    private readonly IUserAvatarRepository _userAvatarRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    private readonly ICurrentUser _currentUser;

    private readonly IHashService _hashService;

    private readonly IEmailChangeService _emailChangeService;

    private readonly IEventPublisher _events;

    private readonly ICacheRepository _cache;

    public UserService(
        IUserRepository userRepository,
        IUserAvatarRepository userAvatarRepository,
        IJsonPatchUpdateService patchUpdateService,
        IMapper mapper,
        ICurrentUser currentUser,
        IHashService hashService,
        IEmailChangeService emailChangeService,
        IEventPublisher events,
        ICacheRepository cache
    )
    {
        _userRepository = userRepository;
        _userAvatarRepository = userAvatarRepository;
        _patchUpdateService = patchUpdateService;
        _mapper = mapper;
        _currentUser = currentUser;
        _hashService = hashService;
        _emailChangeService = emailChangeService;
        _events = events;
        _cache = cache;
    }

    private static string BlockedKey(Ulid userId) => $"user:blocked:{userId}";

    public async Task BlockUserAsync(Ulid id)
    {
        await _userRepository.BlockUserAsync(id);
        await _cache.RemoveAsync(BlockedKey(id));

        await _events.PublishAsync(new UserBlockedEvent(id, _currentUser.UserId));
    }

    public async Task ChangeRoleAsync(Ulid id, UserRoles newRole)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        if (user.IsBlocked)
        {
            throw new UserIsBlockedException();
        }

        if (user.Role != UserRoles.Student)
        {
            throw new CantChangeRoleException();
        }

        var oldRole = user.Role;

        user.Role = newRole;

        await _events.PublishAsync(
            new UserRoleChangedEvent(id, _currentUser.UserId, oldRole, newRole)
        );
    }

    public async Task<UserModel> CreateUserAsync(UserCreationPayload payload)
    {
        var model = _mapper.Map<UserModel>(payload);

        _userRepository.Add(model);

        // Publishing here rather than from the registration endpoint covers every creation path.
        await _events.PublishAsync(new UserRegisteredEvent(model.Id, model.Username, model.Role));

        return model;
    }

    public async Task DeleteUserAsync(string password)
    {
        var currentUserId = _currentUser.UserId ?? throw new UnauthorizedException();

        var user =
            await _userRepository.GetByIdAsync(currentUserId) ?? throw new NotFoundException();

        // No password to confirm with, so say so instead of an unactionable 401.
        if (user.PasswordHash is null)
        {
            throw new BadRequestException(
                "У аккаунта не задан пароль. Задайте пароль, чтобы удалить аккаунт."
            );
        }

        if (!_hashService.VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedException();
        }

        _userRepository.DeleteById(currentUserId);
    }

    public async Task<UserModel?> GetUserByIdAsync(Ulid id)
    {
        var user = await _userRepository.GetWithAvatarAsync(id);

        user.ThrowNotFoundIfNull();

        return user;
    }

    public Task<UserModel?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
    {
        return _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
    }

    public async Task<SearchResult<UserModel>> GetUsersAsync(UserFilter filter)
    {
        var result = await _userRepository.SearchAsync(filter, [new UserWithAvatarSpecification()]);

        return result;
    }

    public async Task<bool> IsBlockedAsync(Ulid id)
    {
        // Cached both ways: "not blocked" is the answer on essentially every authenticated
        // request, so it is the one worth keeping out of the database.
        var cached = await _cache.GetAsync<bool?>(BlockedKey(id));

        if (cached.HasValue)
        {
            return cached.Value;
        }

        var isBlocked = await _userRepository.IsBlockedAsync(id);

        await _cache.SetAsync(BlockedKey(id), isBlocked, _blockedCacheTtl);

        return isBlocked;
    }

    public async Task UnblockUserAsync(Ulid id)
    {
        await _userRepository.UnblockUserAsync(id);
        await _cache.RemoveAsync(BlockedKey(id));

        await _events.PublishAsync(new UserUnblockedEvent(id, _currentUser.UserId));
    }

    public async Task UpdateUserAsync(Ulid id, JsonPatchDocument<UpdateUserDTO> patchUserDto)
    {
        var user = await _userRepository.GetByIdAsync(id);

        user.ThrowNotFoundIfNull();

        if (patchUserDto.ContainsOperation(u => u.Email))
        {
            var (_, newEmail) = patchUserDto.RemoveOperation(u => u.Email);
            await _emailChangeService.RequestAsync(
                user.Id,
                newEmail?.ToString() ?? throw new BadRequestException("Email value is required")
            );
        }

        // Read after the email operation was pulled out above, so this reflects only the fields
        // this patch actually writes.
        var changedFields = patchUserDto
            .Operations.Select(operation => operation.Path?.TrimStart('/'))
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => path!)
            .Distinct()
            .ToArray();

        _patchUpdateService.ApplyPatch(user, patchUserDto);

        await _events.PublishAsync(
            new UserProfileUpdatedEvent(id, _currentUser.UserId, changedFields)
        );
    }

    public async Task UpdateUserAvatarAsync(
        Ulid userId,
        JsonPatchDocument<UpdateUserAvatarDTO> patchAvatarDto
    )
    {
        var userAvatar = await _userAvatarRepository.GetUserAvatarByUserIdAsync(userId);

        if (userAvatar is null)
        {
            userAvatar = new UserAvatarModel { UserId = userId };
            _userAvatarRepository.Add(userAvatar);
        }

        var avatarType = patchAvatarDto.GetValue(u => u.AvatarType);

        if (avatarType == UserAvatarType.Telegram)
        {
            // TODO: validate telegram hash
        }

        _patchUpdateService.ApplyPatch(userAvatar, patchAvatarDto);
    }

    public async Task UpdateUserEmailAsync(Ulid id, string newEmail)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        user.Email = newEmail;
    }

    public async Task UpdateUserPasswordAsync(Ulid id, string newPasswordHash)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        user.PasswordHash = newPasswordHash;
    }

    public Task<bool> UserExistsAsync(string? username, string? email)
    {
        if (username is null && email is null)
        {
            throw new ArgumentException("Username or email must be provided");
        }

        return _userRepository.ExistsByUsernameOrEmailAsync(username, email);
    }

    public async Task VerifyUserAsync(Ulid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        if (user.IsBlocked)
        {
            throw new UserIsBlockedException();
        }

        user.IsVerified = true;

        await _events.PublishAsync(new UserVerifiedEvent(id, _currentUser.UserId));
    }
}
