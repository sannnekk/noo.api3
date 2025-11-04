using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

public interface IUserRepository : IRepository<UserModel>
{
    public Task<bool> ExistsByUsernameOrEmailAsync(string? username, string? email);
    public Task<UserModel?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    public Task<bool> IsBlockedAsync(Ulid id);
    public Task BlockUserAsync(Ulid id);
    public Task UnblockUserAsync(Ulid id);
    public Task<bool> MentorExistsAsync(Ulid mentorId);
    public Task<Dictionary<UserRoles, int>> GetTotalUsersByRolesAsync(DateTime fromDate, DateTime toDate);
    public Task<Dictionary<DateTime, int>> GetRegistrationsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    public Task<List<UserModel>> GetUsersByRoleAsync(UserRoles role);
}
