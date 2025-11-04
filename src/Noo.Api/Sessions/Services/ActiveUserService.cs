using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Sessions.Services;

[RegisterScoped(typeof(IActiveUserService))]
public class ActiveUserService : IActiveUserService
{
    private readonly ICacheRepository _cache;

    private static readonly TimeSpan _activeTtl = SessionConfig.ActiveTtl;

    public ActiveUserService(ICacheRepository cache)
    {
        _cache = cache;
    }

    private static string ActiveUserKey(Ulid userId) => $"active:user:{userId}";
    private static string ActiveRoleUserKey(UserRoles role, Ulid userId) => $"active:role:{role}:{userId}";
    private static string ActivePattern => "active:user:*";
    private static string ActiveRolePattern(UserRoles role) => $"active:role:{role}:*";

    public Task<int> GetActiveCountAsync(UserRoles? role = null)
    {
        if (role.HasValue)
        {
            return _cache.CountAsync(ActiveRolePattern(role.Value));
        }
        return _cache.CountAsync(ActivePattern);
    }

    public Task SetUserActiveAsync(Ulid userId, UserRoles role)
    {
        var now = DateTime.UtcNow;
        var tasks = new List<Task>
        {
            _cache.SetAsync(ActiveUserKey(userId), now, _activeTtl)
        };
        if (Enum.IsDefined(role))
        {
            tasks.Add(_cache.SetAsync(ActiveRoleUserKey(role, userId), now, _activeTtl));
        }
        return Task.WhenAll(tasks);
    }

    public async Task<bool> IsUserActiveAsync(Ulid userId)
    {
        var last = await GetLastActiveByUserAsync(userId);
        return last.HasValue && DateTime.UtcNow - last.Value < _activeTtl;
    }

    public Task<DateTime?> GetLastActiveByUserAsync(Ulid userId)
        => _cache.GetAsync<DateTime?>(ActiveUserKey(userId));

    public Task<Dictionary<UserRoles, int>> GetActiveCountByRolesAsync()
    {
        var tasks = Enum.GetValues<UserRoles>()
            .Select(role => _cache.CountAsync(ActiveRolePattern(role)))
            .ToArray();

        return Task.WhenAll(tasks).ContinueWith(t =>
        {
            var counts = t.Result;
            return Enum.GetValues<UserRoles>()
                .ToDictionary(role => role, role => counts[(int)role]);
        });
    }
}
