using AutoMapper;
using Noo.Api.Auth.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Specifications;
using Noo.Api.Users.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IUserService))]
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    private readonly IUserAvatarRepository _userAvatarRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    private readonly ICurrentUser _currentUser;

    private readonly IHashService _hashService;

    private readonly IEmailChangeService _emailChangeService;

    public UserService(
        IUserRepository userRepository,
        IUserAvatarRepository userAvatarRepository,
        IJsonPatchUpdateService patchUpdateService,
        IMapper mapper,
        ICurrentUser currentUser,
        IHashService hashService,
        IEmailChangeService emailChangeService
    )
    {
        _userRepository = userRepository;
        _userAvatarRepository = userAvatarRepository;
        _patchUpdateService = patchUpdateService;
        _mapper = mapper;
        _currentUser = currentUser;
        _hashService = hashService;
        _emailChangeService = emailChangeService;
    }

    public async Task BlockUserAsync(Ulid id)
    {
        await _userRepository.BlockUserAsync(id);
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

        user.Role = newRole;
    }

    public Ulid CreateUser(UserCreationPayload payload)
    {
        var model = _mapper.Map<UserModel>(payload);

        _userRepository.Add(model);

        return model.Id;
    }

    public async Task DeleteUserAsync(string password)
    {
        var currentUserId = _currentUser.UserId ?? throw new UnauthorizedException();

        var user =
            await _userRepository.GetByIdAsync(currentUserId) ?? throw new NotFoundException();

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

    public Task<bool> IsBlockedAsync(Ulid id)
    {
        return _userRepository.IsBlockedAsync(id);
    }

    public Task UnblockUserAsync(Ulid id)
    {
        return _userRepository.UnblockUserAsync(id);
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

        _patchUpdateService.ApplyPatch(user, patchUserDto);
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
    }
}
