using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Sessions.Services;

public interface IActiveUserService
{
    public Task<int> GetActiveCountAsync(UserRoles? role = null);
    public Task SetUserActiveAsync(Ulid userId, UserRoles role);
    public Task<bool> IsUserActiveAsync(Ulid userId);
    public Task<DateTime?> GetLastActiveByUserAsync(Ulid userId);
    public Task<Dictionary<UserRoles, int>> GetActiveCountByRolesAsync();
}
