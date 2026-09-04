using System.Security.Claims;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Core.Security.Authorization;

/// <summary>
/// Where <see cref="CurrentUser"/> reads the caller from. Defaults to the ambient HTTP request;
/// callers outside one (SignalR hub invocations, which never flow an <see cref="HttpContext"/>)
/// set <see cref="Principal"/> for the lifetime of their scope instead.
/// </summary>
[RegisterScoped]
public class ClaimsPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal { get; set; }

    public ClaimsPrincipal? Current => Principal ?? _httpContextAccessor.HttpContext?.User;
}
