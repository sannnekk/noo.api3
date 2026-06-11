namespace Noo.Api.Media;

/// <summary>
/// The httpOnly cookie that lets the browser authenticate plain <c>&lt;img&gt;</c>
/// requests to <c>/media/{id}/raw</c>, which cannot carry an Authorization header.
///
/// It holds the user's current access token, is scoped to <c>/media</c> (so it is
/// never sent on any other API call) and is <c>SameSite=Lax</c>. Because the front
/// and API share a registrable domain, Lax cookies are still sent on same-site
/// subresource (image) requests, while cross-site requests get nothing.
/// </summary>
public static class MediaCookie
{
    public const string Name = "noo.mediaToken";

    /// <summary>
    /// Authentication scheme that reads and validates the media token from the cookie.
    /// </summary>
    public const string Scheme = "MediaCookie";

    private const string _path = "/media";

    public static void SetMediaToken(
        this HttpResponse response,
        string token,
        DateTime expiresAt,
        bool secure
    )
    {
        response.Cookies.Append(Name, token, BuildOptions(secure, expiresAt));
    }

    public static void ClearMediaToken(this HttpResponse response, bool secure)
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
