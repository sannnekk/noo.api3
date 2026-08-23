using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.MediaDownloads.Services;

namespace Noo.UnitTests.MediaDownloads;

/// <summary>
/// Guards the aggregate queries against MySQL, which the rest of the suite cannot do: the InMemory
/// provider evaluates anything on the client and so accepts groupings MySQL refuses to translate.
/// </summary>
/// <remarks>
/// <c>ToQueryString</c> compiles the query all the way to SQL without opening a connection, so this
/// needs no database — an untranslatable query throws here exactly as it would at runtime.
/// </remarks>
public class MediaDownloadQueryTranslationTests
{
    private static NooDbContext CreateMySqlDb()
    {
        var dbConfig = new DbConfig
        {
            User = "u",
            Password = "p",
            Host = "127.0.0.1",
            Port = "3306",
            Database = "d",
            CommandTimeout = 30,
            DefaultCharset = "utf8mb4",
            DefaultCollation = "utf8mb4_unicode_ci",
        };

        var options = new DbContextOptionsBuilder<NooDbContext>()
            .UseMySql(
                dbConfig.ConnectionString,
                // Pinned rather than auto-detected: detection would need the server to answer.
                new MySqlServerVersion(new Version(8, 0, 36))
            )
            .Options;

        return new NooDbContext(Options.Create(dbConfig), options);
    }

    [Fact]
    public void Counts_By_Media_Translates_To_Sql()
    {
        using var ctx = CreateMySqlDb();
        var repository = new MediaDownloadRepository(ctx);

        var sql = repository.CountsByMediaQuery([Ulid.NewUlid(), Ulid.NewUlid()]).ToQueryString();

        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Downloaders_Translate_To_Sql()
    {
        using var ctx = CreateMySqlDb();
        var repository = new MediaDownloadRepository(ctx);

        var sql = repository.DownloadersQuery(Ulid.NewUlid(), null).ToQueryString();

        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Downloaders_Page_Translates_To_Sql()
    {
        using var ctx = CreateMySqlDb();
        var repository = new MediaDownloadRepository(ctx);

        var sql = repository
            .DownloadersPageQuery(Ulid.NewUlid(), Ulid.NewUlid(), 2, 25)
            .ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
    }
}
