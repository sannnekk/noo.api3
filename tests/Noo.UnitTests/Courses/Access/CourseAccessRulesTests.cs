using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.Api.Courses.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Courses.Access;

/// <summary>
/// The rule behind both the authorization handler and every student-facing course list. A public
/// course must be reachable with no membership row at all — that is the whole point of the audience
/// table — and must stop being reachable the moment the audience row goes.
/// </summary>
public class CourseAccessRulesTests
{
    private static async Task<(NooDbContext Ctx, Ulid CourseId, Ulid StudentId)> SeedAsync(
        Action<CourseModel, Ulid> arrange
    )
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var studentId = Ulid.NewUlid();
        var course = new CourseModel { Name = "Course" };

        arrange(course, studentId);

        ctx.GetDbSet<CourseModel>().Add(course);
        await ctx.SaveChangesAsync();

        return (ctx, course.Id, studentId);
    }

    private static Task<bool> HasAccessAsync(NooDbContext ctx, Ulid courseId, Ulid studentId)
    {
        return new CourseAccessService(ctx).HasAccessAsync(courseId, studentId);
    }

    [Fact]
    public async Task An_Active_Membership_Grants_Access()
    {
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, student) =>
                course.Memberships.Add(
                    new CourseMembershipModel { StudentId = student, IsActive = true }
                )
        );
        using var _ = ctx;

        Assert.True(await HasAccessAsync(ctx, courseId, studentId));
    }

    [Fact]
    public async Task A_Deactivated_Membership_Does_Not_Grant_Access()
    {
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, student) =>
                course.Memberships.Add(
                    new CourseMembershipModel { StudentId = student, IsActive = false }
                )
        );
        using var _ = ctx;

        Assert.False(await HasAccessAsync(ctx, courseId, studentId));
    }

    [Fact]
    public async Task An_Everyone_Audience_Grants_Access_Without_Any_Membership_Row()
    {
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, _) =>
                course.Audiences.Add(
                    new CourseAudienceModel { Kind = CourseAudienceKind.Everyone }
                )
        );
        using var _ = ctx;

        Assert.True(await HasAccessAsync(ctx, courseId, studentId));
        Assert.Empty(ctx.GetDbSet<CourseMembershipModel>());
    }

    [Fact]
    public async Task Removing_The_Everyone_Audience_Revokes_Access_For_Everyone_At_Once()
    {
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, _) =>
                course.Audiences.Add(
                    new CourseAudienceModel { Kind = CourseAudienceKind.Everyone }
                )
        );
        using var _ = ctx;

        var audience = await ctx.GetDbSet<CourseAudienceModel>().SingleAsync();
        ctx.GetDbSet<CourseAudienceModel>().Remove(audience);
        await ctx.SaveChangesAsync();

        Assert.False(await HasAccessAsync(ctx, courseId, studentId));
    }

    [Fact]
    public async Task A_Soft_Deleted_Course_Is_Never_Accessible()
    {
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, _) =>
            {
                course.IsDeleted = true;
                course.Audiences.Add(new CourseAudienceModel { Kind = CourseAudienceKind.Everyone });
            }
        );
        using var _ = ctx;

        Assert.False(await HasAccessAsync(ctx, courseId, studentId));
    }

    [Fact]
    public async Task A_Subscription_Audience_Grants_Nothing_Yet()
    {
        // Reserved for subscriptions and evaluated by no rule, so it must fail closed rather than
        // quietly opening the course to everybody.
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, _) =>
                course.Audiences.Add(
                    new CourseAudienceModel
                    {
                        Kind = CourseAudienceKind.SubscriptionTier,
                        TargetId = Ulid.NewUlid(),
                    }
                )
        );
        using var _ = ctx;

        Assert.False(await HasAccessAsync(ctx, courseId, studentId));
    }

    [Fact]
    public async Task Another_Students_Membership_Does_Not_Leak_Access()
    {
        var (ctx, courseId, studentId) = await SeedAsync(
            (course, _) =>
                course.Memberships.Add(
                    new CourseMembershipModel { StudentId = Ulid.NewUlid(), IsActive = true }
                )
        );
        using var _ = ctx;

        Assert.False(await HasAccessAsync(ctx, courseId, studentId));
    }
}
