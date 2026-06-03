namespace Noo.Api.Auth;

/// <summary>
/// Helpers for the refresh-token cookie. The cookie is httpOnly, scoped to <c>/auth</c> and SameSite=Lax
/// </summary>
public static class RefreshCookie
{
    public const string Name = "noo.refreshToken";

    private const string _path = "/auth";

    public static void SetRefreshToken(
        this HttpResponse response,
        string token,
        DateTime expiresAt,
        bool secure
    )
    {
        response.Cookies.Append(Name, token, BuildOptions(secure, expiresAt));
    }

    public static void ClearRefreshToken(this HttpResponse response, bool secure)
    {
        response.Cookies.Delete(Name, BuildOptions(secure, null));
    }

    private static CookieOptions BuildOptions(bool secure, DateTime? expiresAt) =>
        new()
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = _path,
            Expires = expiresAt,
            IsEssential = true,
        };
}
