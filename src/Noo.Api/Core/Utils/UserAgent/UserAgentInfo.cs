namespace Noo.Api.Core.Utils.UserAgent;

public class UserAgentInfo
{
    public BrowserKind Browser { get; set; } = BrowserKind.Unknown;
    public string? Os { get; set; }
    public string? Device { get; set; }
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
}
