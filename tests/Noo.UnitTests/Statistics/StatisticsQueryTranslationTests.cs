using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.Utils;
using Noo.Api.Users.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Statistics;

/// <summary>
/// Guards every aggregate feeding the statistics endpoints against MySQL. Each has to group and
/// count inside the database: handing a grouping straight to <c>ToDictionary</c> compiles, but
/// makes the server ship every matching row so the arithmetic can happen in the API instead.
/// </summary>
public class StatisticsQueryTranslationTests
{
    private static void AssertAggregatesInSql(string sql, string aggregate)
    {
        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(aggregate, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Total_Users_By_Roles_Counts_In_The_Database()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new UserRepository(ctx);

        AssertAggregatesInSql(repository.TotalUsersByRolesQuery().ToQueryString(), "COUNT(*)");
    }

    [Fact]
    public void Registrations_By_Date_Range_Counts_In_The_Database()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new UserRepository(ctx);

        var sql = repository
            .RegistrationsByDateRangeQuery(Clock.Today.AddDays(-30), Clock.Today)
            .ToQueryString();

        AssertAggregatesInSql(sql, "COUNT(*)");
    }

    [Fact]
    public void Assigned_Works_By_Date_Range_Counts_In_The_Database()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new AssignedWorkRepository(ctx);

        var sql = repository
            .ByDateRangeQuery(aw => aw.Score != null, Clock.Today.AddDays(-30), Clock.Today)
            .ToQueryString();

        AssertAggregatesInSql(sql, "COUNT(*)");
    }

    [Fact]
    public void Month_Average_Scores_Averages_In_The_Database()
    {
        using var ctx = TestHelpers.CreateMySqlDb();
        var repository = new AssignedWorkRepository(ctx);

        var sql = repository.MonthAverageScoresQuery(Ulid.NewUlid(), null).ToQueryString();

        AssertAggregatesInSql(sql, "AVG(");
    }
}
