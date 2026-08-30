using Noo.Api.Core.Utils.Json;

namespace Noo.Api.Core.Utils.UserAgent;

public enum DeviceType
{
    Unknown,
    Desktop,
    Mobile,
    Tablet
}

public static class DeviceTypeExtensions
{
    private static readonly HyphenLowerCaseNamingPolicy _namingPolicy = new();

    /// <summary>
    /// The device type as it appears over the API: hyphenated lowercase, e.g. <c>desktop</c>.
    /// </summary>
    public static string ToWireName(this DeviceType deviceType)
    {
        return _namingPolicy.ConvertName(deviceType.ToString());
    }

    public static string Translate(this DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.Desktop => "Компьютер",
            DeviceType.Mobile => "Телефон",
            DeviceType.Tablet => "Планшет",
            _ => "Другое",
        };
    }
}
