using Moq;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.Api.Media.Access;
using Noo.Api.Media.Access.Rules;
using Noo.Api.Media.Models;
using Noo.Api.Media.Types;
using Noo.Api.Subjects.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Media;

public class CourseAttachmentAccessRuleTests
{
    private static ICurrentUser MakeUser(UserRoles role, Ulid userId)
    {
        var mock = new Mock<ICurrentUser> { CallBase = true };
        mock.SetupGet(m => m.UserId).Returns(userId);
        mock.SetupGet(m => m.UserRole).Returns(role);
        mock.SetupGet(m => m.IsAuthenticated).Returns(true);
        return mock.Object;
    }

    private static CourseAttachmentAccessRule MakeRule(NooDbContext ctx, bool hasMembership)
    {
        var memberships = new Mock<ICourseMembershipService>();
        memberships
            .Setup(m => m.HasAccessAsync(It.IsAny<Ulid>(), It.IsAny<Ulid>()))
            .ReturnsAsync(hasMembership);

        return new CourseAttachmentAccessRule(memberships.Object, new CourseContentRepository(ctx));
    }

    private record Seed(Ulid CourseId, Ulid ContentId);

    private static async Task<Seed> SeedMaterialAsync(NooDbContext ctx)
    {
        var subject = new SubjectModel { Name = "Math", Color = "red" };
        ctx.GetDbSet<SubjectModel>().Add(subject);

        var course = new CourseModel { Name = "Course 1", SubjectId = subject.Id };
        ctx.GetDbSet<CourseModel>().Add(course);

        var chapter = new CourseChapterModel
        {
            Title = "Chapter",
            CourseId = course.Id,
            IsActive = true,
        };
        ctx.GetDbSet<CourseChapterModel>().Add(chapter);

        var content = new CourseMaterialContentModel();
        ctx.GetDbSet<CourseMaterialContentModel>().Add(content);

        var material = new CourseMaterialModel
        {
            Title = "Material",
            ChapterId = chapter.Id,
            ContentId = content.Id,
            IsActive = true,
        };
        ctx.GetDbSet<CourseMaterialModel>().Add(material);

        await ctx.SaveChangesAsync();

        return new Seed(course.Id, content.Id);
    }

    // The uploader tags an attachment with the content id, not the course id.
    private static MediaModel MakeAttachment(Ulid entityId) =>
        new()
        {
            Path = "course-attachment/o/f.pdf",
            Name = "f.pdf",
            ActualName = "f.pdf",
            Extension = "pdf",
            Category = MediaCategory.CourseAttachment,
            Status = MediaStatus.Completed,
            EntityId = entityId,
            OwnerId = Ulid.NewUlid(),
        };

    [Fact]
    public async Task Allows_A_Student_Enrolled_On_The_Owning_Course()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialAsync(ctx);

        var decision = await MakeRule(ctx, hasMembership: true)
            .EvaluateAsync(
                new MediaAccessContext(
                    MakeAttachment(seed.ContentId),
                    MakeUser(UserRoles.Student, Ulid.NewUlid())
                )
            );

        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Denies_A_Student_Not_Enrolled_On_The_Owning_Course()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialAsync(ctx);

        var decision = await MakeRule(ctx, hasMembership: false)
            .EvaluateAsync(
                new MediaAccessContext(
                    MakeAttachment(seed.ContentId),
                    MakeUser(UserRoles.Student, Ulid.NewUlid())
                )
            );

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task Resolves_The_Course_Through_The_Content_The_File_Is_Attached_To()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialAsync(ctx);

        var memberships = new Mock<ICourseMembershipService>();
        memberships
            .Setup(m => m.HasAccessAsync(It.IsAny<Ulid>(), It.IsAny<Ulid>()))
            .ReturnsAsync(true);

        var rule = new CourseAttachmentAccessRule(
            memberships.Object,
            new CourseContentRepository(ctx)
        );

        var studentId = Ulid.NewUlid();
        await rule.EvaluateAsync(
            new MediaAccessContext(
                MakeAttachment(seed.ContentId),
                MakeUser(UserRoles.Student, studentId)
            )
        );

        // The content id must never reach the membership check — that was the original bug.
        memberships.Verify(m => m.HasAccessAsync(seed.CourseId, studentId), Times.Once);
        memberships.Verify(m => m.HasAccessAsync(seed.ContentId, studentId), Times.Never);
    }

    [Fact]
    public async Task Falls_Back_To_Reading_The_Entity_Id_As_A_Course_Id()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var seed = await SeedMaterialAsync(ctx);

        var memberships = new Mock<ICourseMembershipService>();
        memberships
            .Setup(m => m.HasAccessAsync(It.IsAny<Ulid>(), It.IsAny<Ulid>()))
            .ReturnsAsync(true);

        var rule = new CourseAttachmentAccessRule(
            memberships.Object,
            new CourseContentRepository(ctx)
        );

        var studentId = Ulid.NewUlid();
        await rule.EvaluateAsync(
            new MediaAccessContext(
                MakeAttachment(seed.CourseId),
                MakeUser(UserRoles.Student, studentId)
            )
        );

        memberships.Verify(m => m.HasAccessAsync(seed.CourseId, studentId), Times.Once);
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Teacher)]
    [InlineData(UserRoles.Mentor)]
    [InlineData(UserRoles.Assistant)]
    public async Task Staff_Bypass_The_Membership_Check(UserRoles role)
    {
        using var ctx = TestHelpers.CreateInMemoryDb();

        var decision = await MakeRule(ctx, hasMembership: false)
            .EvaluateAsync(
                new MediaAccessContext(
                    MakeAttachment(Ulid.NewUlid()),
                    MakeUser(role, Ulid.NewUlid())
                )
            );

        Assert.True(decision.Allowed);
    }
}
