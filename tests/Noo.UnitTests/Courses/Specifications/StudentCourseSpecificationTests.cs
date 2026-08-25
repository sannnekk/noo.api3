using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.QuerySpecifications;
using Noo.Api.Courses.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Courses.Specifications;

public class StudentCourseSpecificationTests
{
    private static async Task<NooDbContext> SeedAsync(Ulid studentId)
    {
        var ctx = TestHelpers.CreateInMemoryDb();

        var assigned = new CourseModel { Name = "Assigned" };
        assigned.Memberships.Add(new CourseMembershipModel { StudentId = studentId, IsActive = true });

        var open = new CourseModel { Name = "Open" };
        open.Audiences.Add(new CourseAudienceModel { Kind = CourseAudienceKind.Everyone });

        var unrelated = new CourseModel { Name = "Unrelated" };

        ctx.GetDbSet<CourseModel>().AddRange(assigned, open, unrelated);
        await ctx.SaveChangesAsync();

        return ctx;
    }

    private static Task<List<CourseModel>> RunAsync(
        NooDbContext ctx,
        Ulid studentId,
        bool isArchived
    )
    {
        return ctx.GetDbSet<CourseModel>()
            .WithSpecification(new StudentCourseSpecification(studentId, isArchived))
            .ToListAsync();
    }

    [Fact]
    public async Task Lists_Assigned_And_Publicly_Open_Courses_Alike()
    {
        var studentId = Ulid.NewUlid();
        using var ctx = await SeedAsync(studentId);

        var courses = await RunAsync(ctx, studentId, isArchived: false);

        Assert.Equal(["Assigned", "Open"], courses.Select(c => c.Name).Order());
    }

    [Fact]
    public async Task A_Student_With_No_State_Row_Sees_Everything_In_The_Unarchived_Tab()
    {
        // The two tabs must partition the list exhaustively even though most students never
        // create a state row at all.
        var studentId = Ulid.NewUlid();
        using var ctx = await SeedAsync(studentId);

        var active = await RunAsync(ctx, studentId, isArchived: false);
        var archived = await RunAsync(ctx, studentId, isArchived: true);

        Assert.Equal(2, active.Count);
        Assert.Empty(archived);
    }

    [Fact]
    public async Task Archiving_Moves_A_Course_Between_The_Two_Tabs()
    {
        var studentId = Ulid.NewUlid();
        using var ctx = await SeedAsync(studentId);
        var open = await ctx.GetDbSet<CourseModel>().SingleAsync(c => c.Name == "Open");

        ctx.GetDbSet<CourseStudentStateModel>()
            .Add(
                new CourseStudentStateModel
                {
                    CourseId = open.Id,
                    StudentId = studentId,
                    IsArchived = true,
                }
            );
        await ctx.SaveChangesAsync();

        Assert.Equal(["Assigned"], (await RunAsync(ctx, studentId, false)).Select(c => c.Name));
        Assert.Equal(["Open"], (await RunAsync(ctx, studentId, true)).Select(c => c.Name));
    }

    [Fact]
    public async Task Pinned_Courses_Come_First()
    {
        var studentId = Ulid.NewUlid();
        using var ctx = await SeedAsync(studentId);
        var open = await ctx.GetDbSet<CourseModel>().SingleAsync(c => c.Name == "Open");

        ctx.GetDbSet<CourseStudentStateModel>()
            .Add(
                new CourseStudentStateModel
                {
                    CourseId = open.Id,
                    StudentId = studentId,
                    IsPinned = true,
                }
            );
        await ctx.SaveChangesAsync();

        var courses = await RunAsync(ctx, studentId, isArchived: false);

        Assert.Equal("Open", courses[0].Name);
    }

    [Fact]
    public async Task A_Globally_Archived_Course_Is_Hidden_From_Students()
    {
        var studentId = Ulid.NewUlid();
        using var ctx = await SeedAsync(studentId);
        var open = await ctx.GetDbSet<CourseModel>().SingleAsync(c => c.Name == "Open");
        open.IsArchived = true;
        await ctx.SaveChangesAsync();

        var courses = await RunAsync(ctx, studentId, isArchived: false);

        Assert.Equal(["Assigned"], courses.Select(c => c.Name));
    }

    /// <summary>
    /// The InMemory provider evaluates on the client and so accepts shapes MySQL refuses. This
    /// spec leans on filtered includes and a correlated EXISTS inside ORDER BY, which is exactly
    /// where that gap bites, so compile it to SQL as well.
    /// </summary>
    [Fact]
    public void Translates_To_Sql()
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
            .UseMySql(dbConfig.ConnectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        using var ctx = new NooDbContext(Options.Create(dbConfig), options);

        var sql = ctx.GetDbSet<CourseModel>()
            .WithSpecification(new StudentCourseSpecification(Ulid.NewUlid(), false))
            .ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }
}
