using Noo.Api.Core.Security.Authorization;
using Noo.Api.Sessions.DTO;

namespace Noo.Api.Sessions.Services;

public interface IOnlineService
{
    public Task<int> GetOnlineCountAsync(UserRoles? role = null);
    public Task<Dictionary<UserRoles, int>> GetOnlineCountByRolesAsync();
    public Task<DateTime?> GetLastOnlineBySessionAsync(Ulid sessionId);
    public Task<DateTime?> GetLastOnlineByUserAsync(Ulid userId);
    public Task SetSessionOnlineAsync(Ulid sessionId);
    public Task SetUserOnlineAsync(Ulid userId, UserRoles role);
    public Task<bool> IsUserOnlineAsync(Ulid userId);
    public Task<OnlineInfoDTO> GetOnlineInfoAsync(Ulid userId);
}
