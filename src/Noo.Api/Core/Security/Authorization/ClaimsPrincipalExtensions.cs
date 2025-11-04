using System.Security.Claims;

namespace Noo.Api.Core.Security.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static Ulid GetId(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            // For anonymous users, avoid throwing during authorization pipeline; return a safe default
            return Ulid.Empty;
        }

        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ulid.TryParse(raw, out var id) ? id : Ulid.Empty;
    }

    public static UserRoles? GetRole(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            // Return lowest-privilege role for anonymous/malformed principals
            return UserRoles.Student;
        }

        var raw = user.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRoles>(raw, out var role) ? role : null;
    }

    public static Ulid GetSessionId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.Sid)?.Value;
        return Ulid.TryParse(raw, out var id) ? id : Ulid.Empty;
    }
}
