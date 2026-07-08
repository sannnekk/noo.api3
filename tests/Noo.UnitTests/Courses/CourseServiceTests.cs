using AutoMapper;
using Moq;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.Api.Subjects.Models;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;

namespace Noo.UnitTests.Courses;

public class CourseServiceTests
{
    private static IMapper CreateMapper() => MapperTestUtils.CreateAppMapper();

    private static ICurrentUser MakeUser(UserRoles role)
    {
        var mock = new Mock<ICurrentUser>();
        mock.SetupGet(m => m.UserId).Returns(Ulid.NewUlid());
        mock.SetupGet(m => m.UserRole).Returns(role);
        mock.SetupGet(m => m.IsAuthenticated).Returns(true);
        mock.Setup(m => m.IsInRole(It.IsAny<UserRoles[]>()))
            .Returns<UserRoles[]>(roles => roles.Contains(role));
        return mock.Object;
    }

    private static CreateCourseDTO MakeCreateCourseDto(string name = "C# 101") =>
        new()
        {
            Name = name,
            Description = "intro",
            SubjectId = Ulid.NewUlid(),
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
        };

    [Fact]
    public async Task Create_And_GetById_Works()
    {
        var dbName = Guid.NewGuid().ToString();
        Ulid id;
        Ulid subjectId;

        using (var ctx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var subject = new SubjectModel { Name = "Math", Color = "#ffffff" };
            ctx.GetDbSet<SubjectModel>().Add(subject);
            await ctx.SaveChangesAsync();
            subjectId = subject.Id;

            var uow = TestHelpers.CreateUowMock(ctx).Object;
            var courseRepo = new CourseRepository(ctx);
            var courseContentRepo = new CourseContentRepository(ctx);
            var currentUser = MakeUser(UserRoles.Admin);
            var mapper = CreateMapper();
            var jsonPatch = new JsonPatchUpdateService(mapper);
            var service = new CourseService(
                courseRepo,
                courseContentRepo,
                new CourseMaterialReactionRepository(ctx),
                currentUser,
                mapper,
                jsonPatch,
                new EntityReferenceFactory(ctx)
            );

            id = await service.CreateAsync(MakeCreateCourseDto() with { SubjectId = subjectId });
            await uow.CommitAsync();
            Assert.NotEqual(default, id);
        }

        using (var verifyCtx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var verifyUow = TestHelpers.CreateUowMock(verifyCtx).Object;
            var verifyRepo = new CourseRepository(verifyCtx);
            var verifyContentRepo = new CourseContentRepository(verifyCtx);
            var mapper = CreateMapper();
            var jsonPatch = new JsonPatchUpdateService(mapper);
            var verifyService = new CourseService(
                verifyRepo,
                verifyContentRepo,
                new CourseMaterialReactionRepository(verifyCtx),
                MakeUser(UserRoles.Admin),
                mapper,
                jsonPatch,
                new EntityReferenceFactory(verifyCtx)
            );

            var fetched = await verifyService.GetByIdAsync(id, includeInactive: true);
            Assert.NotNull(fetched);
            Assert.Equal("C# 101", fetched!.Name);
            Assert.False(fetched.IsDeleted);
            Assert.NotNull(fetched.Subject);
            Assert.Equal(subjectId, fetched.Subject!.Id);
        }
    }

    [Fact]
    public async Task Search_Respects_UserRole_Specification()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);

        // Seed couple of courses directly
        var c1 = new CourseModel { Name = "A", SubjectId = Ulid.NewUlid() };
        var c2 = new CourseModel { Name = "B", SubjectId = Ulid.NewUlid() };
        uow.Context.GetDbSet<CourseModel>().AddRange(c1, c2);
        await uow.CommitAsync();

        // Admin sees all
        var adminService = new CourseService(
            courseRepo,
            courseContentRepo,
            new CourseMaterialReactionRepository(ctx),
            MakeUser(UserRoles.Admin),
            mapper,
            jsonPatch,
            new EntityReferenceFactory(ctx)
        );
        var adminSearch = await adminService.SearchAsync(
            new CourseFilter { Page = 1, PerPage = 10 }
        );
        Assert.Equal(2, adminSearch.Total);

        // Student sees none unless membership exists
        var student = MakeUser(UserRoles.Student);
        var studentService = new CourseService(
            courseRepo,
            courseContentRepo,
            new CourseMaterialReactionRepository(ctx),
            student,
            mapper,
            jsonPatch,
            new EntityReferenceFactory(ctx)
        );
        var studentSearch = await studentService.SearchAsync(
            new CourseFilter { Page = 1, PerPage = 10 }
        );
        Assert.Equal(0, studentSearch.Total);
    }

    [Fact]
    public async Task Search_Hides_Archived_Courses_Unless_Explicitly_Requested_By_Privileged_Role()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);

        var active = new CourseModel { Name = "Active", SubjectId = Ulid.NewUlid() };
        var archived = new CourseModel
        {
            Name = "Archived",
            SubjectId = Ulid.NewUlid(),
            IsArchived = true,
        };
        uow.Context.GetDbSet<CourseModel>().AddRange(active, archived);
        await uow.CommitAsync();

        CourseService MakeService(UserRoles role) =>
            new(
                courseRepo,
                courseContentRepo,
                new CourseMaterialReactionRepository(ctx),
                MakeUser(role),
                mapper,
                jsonPatch,
                new EntityReferenceFactory(ctx)
            );

        // By default archived courses are hidden for everyone
        var defaultSearch = await MakeService(UserRoles.Admin)
            .SearchAsync(new CourseFilter { Page = 1, PerPage = 10 });
        Assert.Equal(1, defaultSearch.Total);
        Assert.Equal("Active", defaultSearch.Items.Single().Name);

        // Admins and teachers can explicitly request archived courses
        var archivedSearch = await MakeService(UserRoles.Teacher)
            .SearchAsync(new CourseFilter { Page = 1, PerPage = 10, IsArchived = true });
        Assert.Equal(1, archivedSearch.Total);
        Assert.Equal("Archived", archivedSearch.Items.Single().Name);

        // Other roles never see archived courses, even when requested
        var assistantSearch = await MakeService(UserRoles.Assistant)
            .SearchAsync(new CourseFilter { Page = 1, PerPage = 10, IsArchived = true });
        Assert.Equal(1, assistantSearch.Total);
        Assert.Equal("Active", assistantSearch.Items.Single().Name);
    }

    [Fact]
    public async Task SoftDelete_Sets_IsDeleted_True_WhenFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using var ctx = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var currentUser = MakeUser(UserRoles.Admin);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new CourseService(
            courseRepo,
            courseContentRepo,
            new CourseMaterialReactionRepository(ctx),
            currentUser,
            mapper,
            jsonPatch,
            new EntityReferenceFactory(ctx)
        );

        var id = await service.CreateAsync(MakeCreateCourseDto("ToDelete"));
        await uow.CommitAsync();
        await service.SoftDeleteAsync(id);
        await uow.CommitAsync();

        // Verify in fresh context to avoid tracking illusions
        using var verifyCtx = TestHelpers.CreateInMemoryDb(dbName);
        var verifyUow = TestHelpers.CreateUowMock(verifyCtx).Object;
        var course = await verifyUow.Context.GetDbSet<CourseModel>().FindAsync(id);
        Assert.NotNull(course);
        Assert.True(course!.IsDeleted);
    }

    [Fact]
    public async Task SetArchived_Toggles_IsArchived()
    {
        var dbName = Guid.NewGuid().ToString();
        using var ctx = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var currentUser = MakeUser(UserRoles.Admin);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new CourseService(
            courseRepo,
            courseContentRepo,
            new CourseMaterialReactionRepository(ctx),
            currentUser,
            mapper,
            jsonPatch,
            new EntityReferenceFactory(ctx)
        );

        var id = await service.CreateAsync(MakeCreateCourseDto("ToArchive"));
        await uow.CommitAsync();

        await service.SetArchivedAsync(id, true);
        await uow.CommitAsync();

        using (var verifyCtx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var course = await verifyCtx.GetDbSet<CourseModel>().FindAsync(id);
            Assert.NotNull(course);
            Assert.True(course!.IsArchived);
        }

        await service.SetArchivedAsync(id, false);
        await uow.CommitAsync();

        using (var verifyCtx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var course = await verifyCtx.GetDbSet<CourseModel>().FindAsync(id);
            Assert.NotNull(course);
            Assert.False(course!.IsArchived);
        }
    }

    [Fact]
    public async Task SetArchived_Throws_NotFound_WhenCourseMissing()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var mapper = CreateMapper();
        var service = new CourseService(
            new CourseRepository(ctx),
            new CourseContentRepository(ctx),
            new CourseMaterialReactionRepository(ctx),
            MakeUser(UserRoles.Admin),
            mapper,
            new JsonPatchUpdateService(mapper),
            new EntityReferenceFactory(ctx)
        );

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SetArchivedAsync(Ulid.NewUlid(), true)
        );
    }

    [Fact]
    public async Task CreateMaterialContent_Maps_And_Persists()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var currentUser = MakeUser(UserRoles.Admin);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new CourseService(
            courseRepo,
            courseContentRepo,
            new CourseMaterialReactionRepository(ctx),
            currentUser,
            mapper,
            jsonPatch,
            new EntityReferenceFactory(ctx)
        );

        var dto = new CreateCourseMaterialContentDTO
        {
            Content = new Noo.Api.Core.Utils.Richtext.Delta.DeltaRichText(),
        };

        var contentId = await service.CreateMaterialContentAsync(dto);
        await uow.CommitAsync();
        Assert.NotEqual(default, contentId);

        var fetched = await service.GetContentByIdAsync(contentId);
        Assert.NotNull(fetched);
    }

    [Fact]
    public async Task Update_Course_Applies_JsonPatch_And_Persists()
    {
        var dbName = Guid.NewGuid().ToString();
        Ulid id;
        var newSubject = Ulid.NewUlid();
        var newThumb = Ulid.NewUlid();
        DateTime newStart;
        DateTime newEnd;

        using var ctx = TestHelpers.CreateInMemoryDb(dbName);
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var currentUser = MakeUser(UserRoles.Admin);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new CourseService(
            courseRepo,
            courseContentRepo,
            new CourseMaterialReactionRepository(ctx),
            currentUser,
            mapper,
            jsonPatch,
            new EntityReferenceFactory(ctx)
        );

        var original = MakeCreateCourseDto("Initial Name");
        newStart = original.StartDate!.Value.AddDays(2);
        newEnd = original.EndDate!.Value.AddDays(5);
        id = await service.CreateAsync(original);
        await uow.CommitAsync();

        var patch = new JsonPatchDocument<UpdateCourseDTO>();

        patch
            .Replace(x => x.Name, "Updated Name")
            .Replace(x => x.Description, "updated description")
            .Replace(x => x.StartDate, newStart)
            .Replace(x => x.EndDate, newEnd)
            .Replace(x => x.SubjectId, newSubject)
            .Replace(x => x.ThumbnailId, newThumb);

        await service.UpdateAsync(id, patch);
        await uow.CommitAsync();

        var verifyUow = TestHelpers.CreateUowMock(ctx).Object;
        var course = await verifyUow.Context.GetDbSet<CourseModel>().FindAsync(id);
        Assert.NotNull(course);
        Assert.Equal("Updated Name", course!.Name);
        Assert.Equal("updated description", course.Description);
        Assert.Equal(newStart, course.StartDate);
        Assert.Equal(newEnd, course.EndDate);
        Assert.Equal(newSubject, course.SubjectId);
        Assert.Equal(newThumb, course.ThumbnailId);
    }

    [Fact]
    public async Task Update_Content_Applies_JsonPatch_And_Persists()
    {
        var dbName = Guid.NewGuid().ToString();
        Ulid contentId;
        var newWorkId = Ulid.NewUlid();
        var newSolve = DateTime.UtcNow.AddDays(3);
        var newCheck = DateTime.UtcNow.AddDays(7);

        using (var ctx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var uow = TestHelpers.CreateUowMock(ctx).Object;
            var courseRepo = new CourseRepository(ctx);
            var courseContentRepo = new CourseContentRepository(ctx);
            var currentUser = MakeUser(UserRoles.Admin);
            var mapper = CreateMapper();
            var jsonPatch = new JsonPatchUpdateService(mapper);
            var service = new CourseService(
                courseRepo,
                courseContentRepo,
                new CourseMaterialReactionRepository(ctx),
                currentUser,
                mapper,
                jsonPatch,
                new EntityReferenceFactory(ctx)
            );

            contentId = await service.CreateMaterialContentAsync(
                new CreateCourseMaterialContentDTO
                {
                    Content = new Noo.Api.Core.Utils.Richtext.Delta.DeltaRichText(),
                }
            );
            await uow.CommitAsync();

            var patch = new JsonPatchDocument<UpdateCourseMaterialContentDTO>();
#pragma warning disable RCS1201 // Use met
            patch.Replace(x => x.Content, new Noo.Api.Core.Utils.Richtext.Delta.DeltaRichText());
#pragma warning restore RCS1201 // Use method chaining

            await service.UpdateContentAsync(contentId, patch);
            await uow.CommitAsync();
        }

        using (var verifyCtx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var verifyUow = TestHelpers.CreateUowMock(verifyCtx).Object;
            var content = await verifyUow
                .Context.GetDbSet<CourseMaterialContentModel>()
                .FindAsync(contentId);
            Assert.NotNull(content);
            Assert.NotNull(content.Content);
        }
    }
}
