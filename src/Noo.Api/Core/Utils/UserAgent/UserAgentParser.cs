using System.Text.RegularExpressions;

namespace Noo.Api.Core.Utils.UserAgent;

public static class UserAgentParser
{
    private const RegexOptions _options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    // Every Chromium derivative keeps "Chrome/" in its user agent, so its own token has to be
    // matched before Chrome, or all of them are reported as Chrome.
    private static readonly (BrowserKind Kind, Regex Pattern)[] _browserPatterns =
    [
        (BrowserKind.Edge, new(@"Edg(e|A|iOS)?/\d+", _options)),
        (BrowserKind.Yandex, new(@"YaBrowser/\d+", _options)),
        (BrowserKind.Opera, new(@"OPR/\d+|OPiOS/\d+|Opera Mini|Opera/\d+", _options)),
        (BrowserKind.Vivaldi, new(@"Vivaldi/\d+", _options)),
        (BrowserKind.SamsungInternet, new(@"SamsungBrowser/\d+", _options)),
        (BrowserKind.Firefox, new(@"(Firefox|FxiOS)/\d+", _options)),
        (BrowserKind.Chrome, new(@"(Chrome|Chromium|CriOS)/\d+", _options)),
        (BrowserKind.Safari, new(@"Version/\d+.*Safari/", _options)),
        (BrowserKind.InternetExplorer, new(@"MSIE \d+|Trident/\d+", _options)),
    ];

    private static readonly (string Name, Regex Pattern)[] _osPatterns =
    [
        ("Windows", new(@"Windows NT [\d.]+", _options)),
        ("Mac OS", new(@"Mac OS X [\d_]+", _options)),
        ("iOS", new(@"(iPhone|iPad|iPod).*OS [\d_]+", _options)),
        ("Android", new(@"Android [\d.]+", _options)),
        ("Linux", new(@"Linux", _options)),
    ];

    private static readonly Regex _iPad = new(@"iPad", _options);
    private static readonly Regex _iPhone = new(@"iPhone|iPod", _options);
    private static readonly Regex _android = new(@"Android", _options);
    private static readonly Regex _mobileToken = new(@"Mobi", _options);
    private static readonly Regex _tabletHints = new(
        @"Tablet|Nexus 7|Nexus 10|SM-T|Kindle|Silk|PlayBook",
        _options
    );
    private static readonly Regex _desktopOs = new(
        @"Windows NT|Macintosh|Mac OS X|X11|CrOS",
        _options
    );

    public static UserAgentInfo Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new UserAgentInfo
            {
                Browser = BrowserKind.Unknown,
                Os = "Unknown",
                Device = "Unknown",
                DeviceType = DeviceType.Unknown,
            };
        }

        var (deviceType, device) = DetectDevice(userAgent);

        return new UserAgentInfo
        {
            Browser = DetectBrowser(userAgent),
            Os = DetectOs(userAgent),
            Device = device,
            DeviceType = deviceType,
        };
    }

    private static BrowserKind DetectBrowser(string userAgent)
    {
        foreach (var (kind, pattern) in _browserPatterns)
        {
            if (pattern.IsMatch(userAgent))
            {
                return kind;
            }
        }

        return BrowserKind.Unknown;
    }

    private static string DetectOs(string userAgent)
    {
        foreach (var (name, pattern) in _osPatterns)
        {
            if (pattern.IsMatch(userAgent))
            {
                return name;
            }
        }

        return "Unknown";
    }

    private static (DeviceType Type, string Name) DetectDevice(string userAgent)
    {
        if (_iPad.IsMatch(userAgent))
        {
            return (DeviceType.Tablet, "iPad");
        }

        if (_iPhone.IsMatch(userAgent))
        {
            return (DeviceType.Mobile, "iPhone");
        }

        // Android puts "Mobile" in the user agent of phones only, which is the one reliable way
        // of telling an Android tablet from an Android phone.
        if (_android.IsMatch(userAgent))
        {
            return _mobileToken.IsMatch(userAgent)
                ? (DeviceType.Mobile, "Android Phone")
                : (DeviceType.Tablet, "Android Tablet");
        }

        if (_tabletHints.IsMatch(userAgent))
        {
            return (DeviceType.Tablet, "Tablet");
        }

        if (_mobileToken.IsMatch(userAgent))
        {
            return (DeviceType.Mobile, "Mobile");
        }

        if (_desktopOs.IsMatch(userAgent))
        {
            return (DeviceType.Desktop, "Desktop");
        }

        // Crawlers and scripted clients look like nothing else; guessing "desktop" only pollutes
        // the device statistics.
        return (DeviceType.Unknown, "Unknown");
    }
}
