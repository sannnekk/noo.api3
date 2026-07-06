using AutoMapper;
using Moq;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.Api.Courses.Types;
using Noo.Api.Subjects.Models;
using Noo.Api.Users.Models;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Courses;

public class CourseMaterialReactionTests
{
    private static IMapper CreateMapper() => MapperTestUtils.CreateAppMapper();

    private static ICurrentUser MakeUser(UserRoles role, Ulid userId)
    {
        var mock = new Mock<ICurrentUser> { CallBase = true };
        mock.SetupGet(m => m.UserId).Returns(userId);
        mock.SetupGet(m => m.UserRole).Returns(role);
        mock.SetupGet(m => m.IsAuthenticated).Returns(true);
        mock.Setup(m => m.IsInRole(It.IsAny<UserRoles[]>()))
            .Returns<UserRoles[]>(roles => roles.Contains(role));
        return mock.Object;
    }

    private static CourseService MakeService(NooDbContext ctx, ICurrentUser currentUser)
    {
        var mapper = CreateMapper();

        return new CourseService(
            new CourseRepository(ctx),
            new CourseContentRepository(ctx),
            new CourseMaterialReactionRepository(ctx),
            currentUser,
            mapper,
            new JsonPatchUpdateService(mapper),
            new EntityReferenceFactory(ctx)
        );
    }

    private static async Task<(Ulid CourseId, Ulid MaterialId)> SeedCourseWithMaterialAsync(
        NooDbContext ctx
    )
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

        var material = new CourseMaterialModel
        {
            Title = "Material",
            ChapterId = chapter.Id,
            IsActive = true,
        };
        ctx.GetDbSet<CourseMaterialModel>().Add(material);

        await ctx.SaveChangesAsync();

        return (course.Id, material.Id);
    }

    private static Task<CourseMaterialReactionModel?> GetReactionAsync(
        NooDbContext ctx,
        Ulid materialId,
        Ulid userId
    )
    {
        return new CourseMaterialReactionRepository(ctx).GetAsync(materialId, userId);
    }

    [Fact]
    public async Task Toggle_Creates_Reaction()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var (courseId, materialId) = await SeedCourseWithMaterialAsync(ctx);
        var userId = Ulid.NewUlid();
        var service = MakeService(ctx, MakeUser(UserRoles.Student, userId));

        await service.ToggleMaterialReactionAsync(
            courseId,
            materialId,
            CourseMaterialReactionTypes.Check
        );
        await ctx.SaveChangesAsync();

        var reaction = await GetReactionAsync(ctx, materialId, userId);
        Assert.NotNull(reaction);
        Assert.Equal(CourseMaterialReactionTypes.Check, reaction!.Reaction);
    }

    [Fact]
    public async Task Toggle_Same_Reaction_Removes_It()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var (courseId, materialId) = await SeedCourseWithMaterialAsync(ctx);
        var userId = Ulid.NewUlid();
        var service = MakeService(ctx, MakeUser(UserRoles.Student, userId));

        await service.ToggleMaterialReactionAsync(
            courseId,
            materialId,
            CourseMaterialReactionTypes.Check
        );
        await ctx.SaveChangesAsync();

        await service.ToggleMaterialReactionAsync(
            courseId,
            materialId,
            CourseMaterialReactionTypes.Check
        );
        await ctx.SaveChangesAsync();

        var reaction = await GetReactionAsync(ctx, materialId, userId);
        Assert.Null(reaction);
    }

    [Fact]
    public async Task Toggle_Different_Reaction_Switches_It()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var (courseId, materialId) = await SeedCourseWithMaterialAsync(ctx);
        var userId = Ulid.NewUlid();
        var service = MakeService(ctx, MakeUser(UserRoles.Student, userId));

        await service.ToggleMaterialReactionAsync(
            courseId,
            materialId,
            CourseMaterialReactionTypes.Check
        );
        await ctx.SaveChangesAsync();

        await service.ToggleMaterialReactionAsync(
            courseId,
            materialId,
            CourseMaterialReactionTypes.Thinking
        );
        await ctx.SaveChangesAsync();

        var reactions = ctx.GetDbSet<CourseMaterialReactionModel>()
            .Where(r => r.MaterialId == materialId && r.UserId == userId)
            .ToList();
        Assert.Single(reactions);
        Assert.Equal(CourseMaterialReactionTypes.Thinking, reactions[0].Reaction);
    }

    [Fact]
    public async Task Toggle_Throws_NotFound_When_Material_Not_In_Course()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var (_, materialId) = await SeedCourseWithMaterialAsync(ctx);
        var service = MakeService(ctx, MakeUser(UserRoles.Student, Ulid.NewUlid()));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.ToggleMaterialReactionAsync(
                Ulid.NewUlid(),
                materialId,
                CourseMaterialReactionTypes.Check
            )
        );
    }

    [Fact]
    public async Task GetById_Includes_Only_Own_Reaction_For_Student()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var (courseId, materialId) = await SeedCourseWithMaterialAsync(ctx);

        var student = new UserModel
        {
            Name = "Student",
            Username = "student",
            Email = "student@noo.test",
            Role = UserRoles.Student,
            PasswordHash = "hash",
        };
        var otherStudent = new UserModel
        {
            Name = "Other",
            Username = "other",
            Email = "other@noo.test",
            Role = UserRoles.Student,
            PasswordHash = "hash",
        };
        ctx.GetDbSet<UserModel>().AddRange(student, otherStudent);

        ctx.GetDbSet<CourseMaterialReactionModel>()
            .AddRange(
                new CourseMaterialReactionModel
                {
                    MaterialId = materialId,
                    UserId = student.Id,
                    Reaction = CourseMaterialReactionTypes.Thinking,
                },
                new CourseMaterialReactionModel
                {
                    MaterialId = materialId,
                    UserId = otherStudent.Id,
                    Reaction = CourseMaterialReactionTypes.Check,
                }
            );
        await ctx.SaveChangesAsync();

        var service = MakeService(ctx, MakeUser(UserRoles.Student, student.Id));
        var course = await service.GetByIdAsync(courseId, includeInactive: true);

        Assert.NotNull(course);
        var material = course!.Chapters.Single().Materials.Single();
        var reaction = Assert.Single(material.Reactions!);
        Assert.Equal(student.Id, reaction.UserId);
        Assert.Equal(CourseMaterialReactionTypes.Thinking, reaction.Reaction);

        var dto = CreateMapper().Map<CourseMaterialDTO>(material);
        Assert.Equal(CourseMaterialReactionTypes.Thinking, dto.MyReaction);
    }

    [Fact]
    public async Task GetById_Does_Not_Load_Reactions_For_Non_Students()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var (courseId, materialId) = await SeedCourseWithMaterialAsync(ctx);

        ctx.GetDbSet<CourseMaterialReactionModel>()
            .Add(
                new CourseMaterialReactionModel
                {
                    MaterialId = materialId,
                    UserId = Ulid.NewUlid(),
                    Reaction = CourseMaterialReactionTypes.Check,
                }
            );
        await ctx.SaveChangesAsync();

        var service = MakeService(ctx, MakeUser(UserRoles.Admin, Ulid.NewUlid()));
        var course = await service.GetByIdAsync(courseId, includeInactive: true);

        Assert.NotNull(course);
        var material = course!.Chapters.Single().Materials.Single();

        var dto = CreateMapper().Map<CourseMaterialDTO>(material);
        Assert.Null(dto.MyReaction);
    }
}
