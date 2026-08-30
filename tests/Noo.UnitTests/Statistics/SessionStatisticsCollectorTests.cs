using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.UserAgent;
using Noo.Api.Sessions.Models;
using Noo.Api.Sessions.Services;
using Noo.Api.Statistics.DTO;
using Noo.Api.Statistics.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Statistics;

public class SessionStatisticsCollectorTests
{
    private static readonly DateTime _from = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime _to = new(2026, 8, 31, 23, 59, 59, DateTimeKind.Unspecified);

    private static (SessionStatisticsCollector collector, NooDbContext ctx) Create()
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        return (new SessionStatisticsCollector(new SessionRepository(ctx)), ctx);
    }

    private static SessionModel Session(
        Ulid userId,
        BrowserKind browser,
        DeviceType deviceType,
        DateTime lastRequestAt
    )
    {
        return new SessionModel
        {
            Id = Ulid.NewUlid(),
            UserId = userId,
            Browser = browser.ToString(),
            DeviceType = deviceType,
            LastRequestAt = lastRequestAt,
        };
    }

    private static StatisticsDistributionDTO Distribution(StatisticsBlockDTO block, string title)
    {
        return block.Distributions.Single(d => d.Title == title);
    }

    [Fact]
    public async Task Counts_Every_User_Once_Per_Browser()
    {
        var (collector, ctx) = Create();
        var user = Ulid.NewUlid();
        var other = Ulid.NewUlid();

        ctx.AddRange(
            // The same user on the same browser twice: one browser, one user.
            Session(user, BrowserKind.Chrome, DeviceType.Desktop, _from.AddDays(1)),
            Session(user, BrowserKind.Chrome, DeviceType.Mobile, _from.AddDays(2)),
            // ...and on a second browser, where they count again.
            Session(user, BrowserKind.Yandex, DeviceType.Desktop, _from.AddDays(3)),
            Session(other, BrowserKind.Chrome, DeviceType.Desktop, _from.AddDays(4))
        );
        await ctx.SaveChangesAsync();

        var block = await collector.GetDeviceStatisticsAsync(_from, _to);
        var browsers = Distribution(block, "Браузеры");

        Assert.Collection(
            browsers.Entries,
            chrome =>
            {
                Assert.Equal("Chrome", chrome.Label);
                Assert.Equal("chrome", chrome.Icon);
                Assert.Equal(2, chrome.Value);
            },
            yandex =>
            {
                Assert.Equal("Яндекс Браузер", yandex.Label);
                Assert.Equal("yandex", yandex.Icon);
                Assert.Equal(1, yandex.Value);
            }
        );
    }

    [Fact]
    public async Task Lists_Every_Device_Type_Largest_First()
    {
        var (collector, ctx) = Create();

        ctx.AddRange(
            Session(Ulid.NewUlid(), BrowserKind.Safari, DeviceType.Mobile, _from.AddDays(1)),
            Session(Ulid.NewUlid(), BrowserKind.Safari, DeviceType.Mobile, _from.AddDays(1)),
            Session(Ulid.NewUlid(), BrowserKind.Chrome, DeviceType.Desktop, _from.AddDays(1))
        );
        await ctx.SaveChangesAsync();

        var deviceTypes = Distribution(
            await collector.GetDeviceStatisticsAsync(_from, _to),
            "Типы устройств"
        );

        Assert.Equal(
            ["Телефон", "Компьютер", "Другое", "Планшет"],
            deviceTypes.Entries.Select(e => e.Label)
        );
        Assert.Equal([2d, 1d, 0d, 0d], deviceTypes.Entries.Select(e => e.Value));
        Assert.Equal(
            ["mobile", "desktop", "unknown", "tablet"],
            deviceTypes.Entries.Select(e => e.Icon)
        );
    }

    [Fact]
    public async Task Leaves_Out_Sessions_From_Outside_The_Period()
    {
        var (collector, ctx) = Create();

        ctx.AddRange(
            Session(Ulid.NewUlid(), BrowserKind.Chrome, DeviceType.Desktop, _from.AddDays(-1)),
            Session(Ulid.NewUlid(), BrowserKind.Chrome, DeviceType.Desktop, _to.AddDays(1)),
            Session(Ulid.NewUlid(), BrowserKind.Chrome, DeviceType.Desktop, _to)
        );
        await ctx.SaveChangesAsync();

        var browsers = Distribution(
            await collector.GetDeviceStatisticsAsync(_from, _to),
            "Браузеры"
        );

        Assert.Equal(1, browsers.Entries.Single().Value);
    }

    [Fact]
    public async Task Folds_Browsers_It_No_Longer_Knows_Into_Other()
    {
        var (collector, ctx) = Create();
        var stale = Session(Ulid.NewUlid(), BrowserKind.Chrome, DeviceType.Desktop, Clock.Now);
        stale.Browser = "Netscape";
        stale.LastRequestAt = _from.AddDays(1);

        ctx.Add(stale);
        await ctx.SaveChangesAsync();

        var browsers = Distribution(
            await collector.GetDeviceStatisticsAsync(_from, _to),
            "Браузеры"
        );

        var entry = browsers.Entries.Single();
        Assert.Equal("Другой", entry.Label);
        Assert.Equal("unknown", entry.Icon);
    }
}
