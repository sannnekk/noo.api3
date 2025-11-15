using AutoMapper;
using Moq;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.UnitTests.Common;
using SystemTextJsonPatch;
using Noo.Api.Courses;
using Noo.Api.Core.Request.Patching;

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
        mock.Setup(m => m.IsInRole(It.IsAny<UserRoles[]>())).Returns<UserRoles[]>(roles => roles.Contains(role));
        return mock.Object;
    }

    private static CreateCourseDTO MakeCreateCourseDto(string name = "C# 101") => new()
    {
        Name = name,
        Description = "intro",
        SubjectId = Ulid.NewUlid(),
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(30)
    };

    [Fact]
    public async Task Create_And_GetById_Works()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var courseRepo = new CourseRepository(ctx);
        var courseContentRepo = new CourseContentRepository(ctx);
        var currentUser = MakeUser(UserRoles.Admin);
        var mapper = CreateMapper();
        var jsonPatch = new JsonPatchUpdateService(mapper);
        var service = new CourseService(uow, courseRepo, courseContentRepo, currentUser, mapper, jsonPatch);

        var id = await service.CreateAsync(MakeCreateCourseDto());
        Assert.NotEqual(default, id);

        var fetched = await service.GetByIdAsync(id, includeInactive: true);
        Assert.NotNull(fetched);
        Assert.Equal("C# 101", fetched!.Name);
        Assert.False(fetched.IsDeleted);
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
        var adminService = new CourseService(uow, courseRepo, courseContentRepo, MakeUser(UserRoles.Admin), mapper, jsonPatch);
        var adminSearch = await adminService.SearchAsync(new CourseFilter { Page = 1, PerPage = 10 });
        Assert.Equal(2, adminSearch.Total);

        // Student sees none unless membership exists
        var student = MakeUser(UserRoles.Student);
        var studentService = new CourseService(uow, courseRepo, courseContentRepo, student, mapper, jsonPatch);
        var studentSearch = await studentService.SearchAsync(new CourseFilter { Page = 1, PerPage = 10 });
        Assert.Equal(0, studentSearch.Total);
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
        var service = new CourseService(uow, courseRepo, courseContentRepo, currentUser, mapper, jsonPatch);

        var id = await service.CreateAsync(MakeCreateCourseDto("ToDelete"));
        await service.SoftDeleteAsync(id);

        // Verify in fresh context to avoid tracking illusions
        using var verifyCtx = TestHelpers.CreateInMemoryDb(dbName);
        var verifyUow = TestHelpers.CreateUowMock(verifyCtx).Object;
        var course = await verifyUow.Context.GetDbSet<CourseModel>().FindAsync(id);
        Assert.NotNull(course);
        Assert.True(course!.IsDeleted);
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
        var service = new CourseService(uow, courseRepo, courseContentRepo, currentUser, mapper, jsonPatch);

        var dto = new CreateCourseMaterialContentDTO
        {
            Content = new Noo.Api.Core.Utils.Richtext.Delta.DeltaRichText(),
            IsWorkAvailable = true,
            WorkSolveDeadlineAt = DateTime.UtcNow.AddDays(1)
        };

        var contentId = await service.CreateMaterialContentAsync(dto);
        Assert.NotEqual(default, contentId);

        var fetched = await service.GetContentByIdAsync(contentId);
        Assert.NotNull(fetched);
        Assert.True(fetched!.IsWorkAvailable);
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
        var service = new CourseService(uow, courseRepo, courseContentRepo, currentUser, mapper, jsonPatch);

        var original = MakeCreateCourseDto("Initial Name");
        newStart = original.StartDate!.Value.AddDays(2);
        newEnd = original.EndDate!.Value.AddDays(5);
        id = await service.CreateAsync(original);

        var patch = new JsonPatchDocument<UpdateCourseDTO>();

        patch.Replace(x => x.Name, "Updated Name")
            .Replace(x => x.Description, "updated description")
            .Replace(x => x.StartDate, newStart)
            .Replace(x => x.EndDate, newEnd)
            .Replace(x => x.SubjectId, newSubject)
            .Replace(x => x.ThumbnailId, newThumb);

        await service.UpdateAsync(id, patch);

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
            var service = new CourseService(uow, courseRepo, courseContentRepo, currentUser, mapper, jsonPatch);

            contentId = await service.CreateMaterialContentAsync(new CreateCourseMaterialContentDTO
            {
                Content = new Noo.Api.Core.Utils.Richtext.Delta.DeltaRichText(),
                IsWorkAvailable = false
            });

            var patch = new JsonPatchDocument<UpdateCourseMaterialContentDTO>();
#pragma warning disable RCS1201 // Use method chaining
            patch.Replace(x => x.IsWorkAvailable, true);
            patch.Replace(x => x.WorkId, newWorkId);
            patch.Replace(x => x.WorkSolveDeadlineAt, newSolve);
            patch.Replace(x => x.WorkCheckDeadlineAt, newCheck);
            patch.Replace(x => x.Content, new Noo.Api.Core.Utils.Richtext.Delta.DeltaRichText());
#pragma warning restore RCS1201 // Use method chaining

            await service.UpdateContentAsync(contentId, patch);
        }

        using (var verifyCtx = TestHelpers.CreateInMemoryDb(dbName))
        {
            var verifyUow = TestHelpers.CreateUowMock(verifyCtx).Object;
            var content = await verifyUow.Context.GetDbSet<CourseMaterialContentModel>().FindAsync(contentId);
            Assert.NotNull(content);
            Assert.True(content!.IsWorkAvailable);
            Assert.Equal(newWorkId, content.WorkId);
            Assert.Equal(newSolve, content.WorkSolveDeadlineAt);
            Assert.Equal(newCheck, content.WorkCheckDeadlineAt);
            Assert.NotNull(content.Content);
        }
    }
}
