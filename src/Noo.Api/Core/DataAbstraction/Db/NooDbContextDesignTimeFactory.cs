using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Initialization.Configuration;

namespace Noo.Api.Core.DataAbstraction.Db;

/// <summary>
/// The context <c>dotnet ef</c> builds, as opposed to the one that serves requests.
/// It reads the same configuration but does not take the app's command timeout with it:
/// that one is sized for a web request, while a single migration statement may rewrite a
/// table of millions of rows and legitimately run for minutes.
/// </summary>
public class NooDbContextDesignTimeFactory : IDesignTimeDbContextFactory<NooDbContext>
{
    /// <summary>
    /// How long one migration statement may take. Deliberately generous: a migration that
    /// is slow should finish and be recorded, not time out halfway and leave the database
    /// changed with nothing in the history to say so.
    /// </summary>
    private static readonly int _commandTimeoutSeconds = (int)
        TimeSpan.FromHours(1).TotalSeconds;

    public NooDbContext CreateDbContext(string[] args)
    {
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var dbConfig = configuration.GetSection(DbConfig.SectionName).GetOrThrow<DbConfig>();

        var options = new DbContextOptionsBuilder<NooDbContext>()
            .UseMySql(
                dbConfig.ConnectionString,
                ServerVersion.AutoDetect(dbConfig.ConnectionString),
                builder =>
                    builder
                        .CommandTimeout(_commandTimeoutSeconds)
                        .EnableIndexOptimizedBooleanColumns()
            )
            .Options;

        return new NooDbContext(Options.Create(dbConfig), options);
    }
}
