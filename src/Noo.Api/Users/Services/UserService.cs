using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IUserService))]
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IUserRepository _userRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    public UserService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IJsonPatchUpdateService patchUpdateService,
        IMapper mapper
    )
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _patchUpdateService = patchUpdateService;
        _mapper = mapper;
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

        _userRepository.Update(user);
        await _unitOfWork.CommitAsync();
    }

    public async Task<Ulid> CreateUserAsync(UserCreationPayload payload)
    {
        var model = _mapper.Map<UserModel>(payload);

        _userRepository.Add(model);
        await _unitOfWork.CommitAsync();

        return model.Id;
    }

    public async Task DeleteUserAsync(Ulid id)
    {
        _userRepository.DeleteById(id);
        await _unitOfWork.CommitAsync();
    }

    public Task<UserModel?> GetUserByIdAsync(Ulid id)
    {
        return _userRepository.GetByIdAsync(id);
    }

    public Task<UserModel?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
    {
        return _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
    }

    public Task<SearchResult<UserModel>> GetUsersAsync(UserFilter filter)
    {
        return _userRepository.SearchAsync(filter);
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
        var model = await _userRepository.GetByIdAsync(id) ?? throw new NotFoundException();

        model.ThrowNotFoundIfNull();

        _patchUpdateService.ApplyPatch(model, patchUserDto);

        _userRepository.Update(model);
        await _unitOfWork.CommitAsync();
    }

    public async Task UpdateUserEmailAsync(Ulid id, string newEmail)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        user.Email = newEmail;

        _userRepository.Update(user);
        await _unitOfWork.CommitAsync();
    }

    public async Task UpdateUserPasswordAsync(Ulid id, string newPasswordHash)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
        {
            throw new NotFoundException();
        }

        user.PasswordHash = newPasswordHash;

        _userRepository.Update(user);
        await _unitOfWork.CommitAsync();
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

        _userRepository.Update(user);
        await _unitOfWork.CommitAsync();
    }
}
