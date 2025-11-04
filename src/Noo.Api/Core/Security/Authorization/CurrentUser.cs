using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Security.Authorization;

[RegisterScoped(typeof(ICurrentUser))]
public class CurrentUser : ICurrentUser
{
    public Ulid? UserId { get; init; }
    public UserRoles? UserRole { get; init; }
    public bool IsAuthenticated { get; init; }

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        UserId = httpContextAccessor.HttpContext?.User.GetId();
        UserRole = httpContextAccessor.HttpContext?.User.GetRole();
        IsAuthenticated = httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }

    public bool IsInRole(params UserRoles[] roles)
    {
        return roles.Any(role => UserRole == role);
    }
}
