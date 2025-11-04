using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(IOnlineService))]
public class OnlineService : IOnlineService
{
    private readonly ICacheRepository _cache;
    private readonly NooDbContext _db;
    private static readonly TimeSpan _onlineTtl = SessionConfig.OnlineTtl;

    public OnlineService(ICacheRepository cache, NooDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    private static string SessionKey(Ulid sessionId) => $"online:session:{sessionId}";
    private static string UserKey(Ulid userId) => $"online:user:{userId}";
    private static string OnlineUsersPattern => "online:user:*";
    private static string OnlineRoleUserKey(UserRoles role, Ulid userId) => $"online:role:{role}:{userId}";
    private static string OnlineRolePattern(UserRoles role) => $"online:role:{role}:*";

    public Task<DateTime?> GetLastOnlineBySessionAsync(Ulid sessionId)
        => _cache.GetAsync<DateTime?>(SessionKey(sessionId));

    public Task<DateTime?> GetLastOnlineByUserAsync(Ulid userId)
        => _cache.GetAsync<DateTime?>(UserKey(userId));

    public Task<int> GetOnlineCountAsync(UserRoles? role = null)
    {
        if (role.HasValue)
        {
            return _cache.CountAsync(OnlineRolePattern(role.Value));
        }
        return _cache.CountAsync(OnlineUsersPattern);
    }

    public Task SetSessionOnlineAsync(Ulid sessionId)
    {
        var now = DateTime.UtcNow;
        return _cache.SetAsync(SessionKey(sessionId), now, _onlineTtl);
    }

    // Parameterless overload: only set the generic user online key; do NOT attribute to a role implicitly.
    public Task SetUserOnlineAsync(Ulid userId)
    {
        var now = DateTime.UtcNow;
        return _cache.SetAsync(UserKey(userId), now, _onlineTtl);
    }

    public Task SetUserOnlineAsync(Ulid userId, UserRoles role)
    {
        var now = DateTime.UtcNow;
        // Keys: base user online + role-specific online. Active tracking handled by ActiveUserService.
        var tasks = new List<Task>
        {
            _cache.SetAsync(UserKey(userId), now, _onlineTtl)
        };
        // Add role-specific key if role provided (avoid enum default misuse by checking defined enum)
        if (Enum.IsDefined(typeof(UserRoles), role))
        {
            tasks.Add(_cache.SetAsync(OnlineRoleUserKey(role, userId), now, _onlineTtl));
        }
        return Task.WhenAll(tasks);
    }

    public async Task<bool> IsUserOnlineAsync(Ulid userId)
    {
        var last = await GetLastOnlineByUserAsync(userId);
        return last.HasValue && DateTime.UtcNow - last.Value < _onlineTtl;
    }

    public Task<Dictionary<UserRoles, int>> GetOnlineCountByRolesAsync()
    {
        var tasks = Enum.GetValues<UserRoles>()
            .Select(role => _cache.CountAsync(OnlineRolePattern(role)))
            .ToArray();

        return Task.WhenAll(tasks).ContinueWith(t =>
        {
            var counts = t.Result;
            return Enum.GetValues<UserRoles>()
                .ToDictionary(role => role, role => counts[(int)role]);
        });
    }
}
