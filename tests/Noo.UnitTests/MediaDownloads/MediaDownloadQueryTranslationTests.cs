using Microsoft.EntityFrameworkCore;
using Noo.Api.MediaDownloads.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.MediaDownloads;

/// <summary>
/// Guards the aggregate queries against MySQL, which the rest of the suite cannot do: the InMemory
/// provider evaluates anything on the client and so accepts groupings MySQL refuses to translate.
/// </summary>
public class MediaDownloadQueryTranslationTests
{
    [Fact]
    public void Counts_By_Media_Translates_To_Sql()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new MediaDownloadRepository(ctx);

        var sql = repository.CountsByMediaQuery([Ulid.NewUlid(), Ulid.NewUlid()]).ToQueryString();

        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Downloaders_Translate_To_Sql()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new MediaDownloadRepository(ctx);

        var sql = repository.DownloadersQuery(Ulid.NewUlid(), null).ToQueryString();

        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Downloaders_Page_Translates_To_Sql()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new MediaDownloadRepository(ctx);

        var sql = repository
            .DownloadersPageQuery(Ulid.NewUlid(), Ulid.NewUlid(), 2, 25)
            .ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
    }
}
