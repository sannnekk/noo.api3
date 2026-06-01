using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.UserAgent;
using Noo.Api.Sessions.Models;

namespace Noo.Api.Sessions.Utils;

public static class HttpContextExtensions
{
    public static SessionModel AsSessionModel(this HttpContext context, Ulid userId)
    {
        if (context is null || context.User is null)
        {
            throw new ArgumentNullException(nameof(context), "HttpContext or User cannot be null.");
        }

        var deviceId = context.Request.Headers["X-Device-Id"].ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var info = string.IsNullOrWhiteSpace(userAgent)
            ? new UserAgentInfo { Browser = "Unknown", Os = "Unknown", Device = "Unknown", DeviceType = DeviceType.Unknown }
            : UserAgentParser.Parse(userAgent);

        return new SessionModel
        {
            UserId = userId,
            UserAgent = userAgent,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
            Os = info.Os,
            Browser = info.Browser,
            Device = info.Device,
            DeviceType = info.DeviceType,
            IpAddress = ip,
            LastRequestAt = Clock.Now
        };
    }
}
