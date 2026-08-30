using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.UnitTests.Common;

public static class TestHelpers
{
    public static NooDbContext CreateInMemoryDb(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<NooDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var dbConfig = Options.Create(new DbConfig
        {
            User = "u",
            Password = "p",
            Host = "h",
            Port = "3306",
            Database = "d",
            CommandTimeout = 30,
            DefaultCharset = "utf8mb4",
            DefaultCollation = "utf8mb4_unicode_ci"
        });

        return new NooDbContext(dbConfig, options);
    }

    /// <summary>
    /// A context on the real MySQL provider, for compiling a query to SQL with
    /// <c>ToQueryString</c>. Nothing may be executed against it — no server is there to answer —
    /// but translation happens without a connection, so an untranslatable query throws exactly as
    /// it would at runtime. The InMemory provider cannot tell the two apart: it evaluates
    /// everything on the client.
    /// </summary>
    public static NooDbContext CreateMySqlDb()
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
            DefaultCollation = "utf8mb4_unicode_ci"
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

    public static Mock<IUnitOfWork> CreateUowMock(NooDbContext ctx)
    {
        var mock = new Mock<IUnitOfWork>();
        mock.SetupGet(u => u.Context).Returns(ctx);
        mock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => ctx.SaveChangesAsync(ct));
        mock.Setup(u => u.Rollback());
        mock.Setup(u => u.Dispose());
        return mock;
    }
}
