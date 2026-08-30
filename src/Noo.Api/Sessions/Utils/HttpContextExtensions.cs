using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.UserAgent;
using Noo.Api.Sessions.Models;

namespace Noo.Api.Sessions.Utils;

public static class HttpContextExtensions
{
    private const int _userAgentMaxLength = 255;

    public static SessionModel AsSessionModel(this HttpContext context, Ulid userId)
    {
        if (context is null || context.User is null)
        {
            throw new ArgumentNullException(nameof(context), "HttpContext or User cannot be null.");
        }

        var deviceId = context.Request.Headers["X-Device-Id"].ToString();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Overlong user agents do exist and the column only holds 255 characters; truncating here
        // keeps both the insert and the lookup by user agent working on the same value.
        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (userAgent.Length > _userAgentMaxLength)
        {
            userAgent = userAgent[.._userAgentMaxLength];
        }

        var info = UserAgentParser.Parse(userAgent);

        return new SessionModel
        {
            UserId = userId,
            UserAgent = userAgent,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
            Os = info.Os,
            Browser = info.Browser.ToString(),
            Device = info.Device,
            DeviceType = info.DeviceType,
            IpAddress = ip,
            LastRequestAt = Clock.Now
        };
    }
}
