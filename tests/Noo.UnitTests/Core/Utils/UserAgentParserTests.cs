using Noo.Api.Core.Utils.UserAgent;

namespace Noo.UnitTests.Core.Utils;

public class UserAgentParserTests
{
    // Chromium derivatives all carry "Chrome/" as well, which is what makes their order matter.
    [Theory]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        BrowserKind.Chrome
    )]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 YaBrowser/23.11.0.0 Safari/537.36",
        BrowserKind.Yandex
    )]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 OPR/106.0.0.0",
        BrowserKind.Opera
    )]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
        BrowserKind.Edge
    )]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Vivaldi/6.5",
        BrowserKind.Vivaldi
    )]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 13; SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/23.0 Chrome/115.0.0.0 Mobile Safari/537.36",
        BrowserKind.SamsungInternet
    )]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        BrowserKind.Firefox
    )]
    [InlineData(
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
        BrowserKind.Safari
    )]
    [InlineData(
        "Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko",
        BrowserKind.InternetExplorer
    )]
    [InlineData("SomeBot/1.0 (+https://example.com/bot)", BrowserKind.Unknown)]
    public void Detects_The_Browser(string userAgent, BrowserKind expected)
    {
        Assert.Equal(expected, UserAgentParser.Parse(userAgent).Browser);
    }

    [Theory]
    [InlineData(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        DeviceType.Desktop
    )]
    [InlineData(
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        DeviceType.Desktop
    )]
    [InlineData(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        DeviceType.Mobile
    )]
    [InlineData(
        "Mozilla/5.0 (iPad; CPU OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        DeviceType.Tablet
    )]
    [InlineData(
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        DeviceType.Mobile
    )]
    // No "Mobile" token: Android says tablet that way and nothing else.
    [InlineData(
        "Mozilla/5.0 (Linux; Android 13; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        DeviceType.Tablet
    )]
    [InlineData("SomeBot/1.0 (+https://example.com/bot)", DeviceType.Unknown)]
    public void Detects_The_Device_Type(string userAgent, DeviceType expected)
    {
        Assert.Equal(expected, UserAgentParser.Parse(userAgent).DeviceType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Falls_Back_To_Unknown_Without_A_User_Agent(string? userAgent)
    {
        var info = UserAgentParser.Parse(userAgent);

        Assert.Equal(BrowserKind.Unknown, info.Browser);
        Assert.Equal(DeviceType.Unknown, info.DeviceType);
    }
}
