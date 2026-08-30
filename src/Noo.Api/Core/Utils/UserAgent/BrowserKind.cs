using Noo.Api.Core.Utils.Json;

namespace Noo.Api.Core.Utils.UserAgent;

public enum BrowserKind
{
    Unknown,
    Chrome,
    Firefox,
    Safari,
    Edge,
    Opera,
    Yandex,
    Vivaldi,
    SamsungInternet,
    InternetExplorer
}

public static class BrowserKindExtensions
{
    private static readonly HyphenLowerCaseNamingPolicy _namingPolicy = new();

    /// <summary>
    /// The browser as it appears over the API: hyphenated lowercase, e.g. <c>samsung-internet</c>.
    /// </summary>
    public static string ToWireName(this BrowserKind browser)
    {
        return _namingPolicy.ConvertName(browser.ToString());
    }

    public static string Translate(this BrowserKind browser)
    {
        return browser switch
        {
            BrowserKind.Chrome => "Chrome",
            BrowserKind.Firefox => "Firefox",
            BrowserKind.Safari => "Safari",
            BrowserKind.Edge => "Edge",
            BrowserKind.Opera => "Opera",
            BrowserKind.Yandex => "Яндекс Браузер",
            BrowserKind.Vivaldi => "Vivaldi",
            BrowserKind.SamsungInternet => "Samsung Internet",
            BrowserKind.InternetExplorer => "Internet Explorer",
            _ => "Другой",
        };
    }
}
