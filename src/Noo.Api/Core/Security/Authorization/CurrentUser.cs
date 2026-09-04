using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Security.Authorization;

[RegisterScoped(typeof(ICurrentUser))]
public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipalAccessor _principalAccessor;

    public CurrentUser(ClaimsPrincipalAccessor principalAccessor)
    {
        _principalAccessor = principalAccessor;
    }

    // Resolved per read, not in the constructor: SignalR builds a hub and its dependencies
    // before the filter that supplies the principal runs, so a value captured at construction
    // time would report an anonymous user for every hub invocation.
    public Ulid? UserId => _principalAccessor.Current?.GetId();

    public UserRoles? UserRole => _principalAccessor.Current?.GetRole();

    public bool IsAuthenticated => _principalAccessor.Current?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(params UserRoles[] roles)
    {
        var role = UserRole;

        return roles.Any(r => role == r);
    }
}
