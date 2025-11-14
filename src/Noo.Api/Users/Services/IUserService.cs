using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Users.Services;

public interface IUserService
{
    public Task<bool> UserExistsAsync(string? username, string? email);
    public Task<Ulid> CreateUserAsync(UserCreationPayload payload);
    public Task<UserModel?> GetUserByIdAsync(Ulid id);
    public Task<UserModel?> GetUserByUsernameOrEmailAsync(string usernameOrEmail);
    public Task<SearchResult<UserModel>> GetUsersAsync(UserFilter filter);
    public Task ChangeRoleAsync(Ulid id, UserRoles newRole);
    public Task UpdateUserPasswordAsync(Ulid id, string newPasswordHash);
    public Task UpdateUserEmailAsync(Ulid id, string newEmail);
    public Task<bool> IsBlockedAsync(Ulid id);
    public Task VerifyUserAsync(Ulid id);
    public Task BlockUserAsync(Ulid id);
    public Task UnblockUserAsync(Ulid id);
    public Task DeleteUserAsync(Ulid id);
    public Task UpdateUserAsync(Ulid id, JsonPatchDocument<UpdateUserDTO> patchUserDto);
}
