using AutoMapper;
using Moq;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Exceptions;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.Courses.Services;

public class CourseMembershipServiceTests
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

    private static (CourseMembershipService svc, Noo.Api.Core.DataAbstraction.Db.NooDbContext ctx, Noo.Api.Core.DataAbstraction.Db.IUnitOfWork uow) CreateService()
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var svc = new CourseMembershipService(
            new CourseMembershipRepository(ctx),
            CreateMapper(),
            MakeUser(UserRoles.Admin),
            new Mock<IEventPublisher>().Object
        );

        return (svc, ctx, uow);
    }

    [Fact]
    public async Task Putting_A_Student_On_A_Course_They_Are_Already_On_Is_Refused()
    {
        var (svc, ctx, uow) = CreateService();
        using var _ = ctx;
        var dto = new CreateCourseMembershipDTO
        {
            CourseId = Ulid.NewUlid(),
            StudentId = Ulid.NewUlid()
        };

        await svc.CreateMembershipAsync(dto);
        await uow.CommitAsync();

        await Assert.ThrowsAsync<StudentAlreadyOnCourseException>(
            () => svc.CreateMembershipAsync(dto)
        );

        Assert.Single(ctx.GetDbSet<CourseMembershipModel>());
    }

    [Fact]
    public async Task Putting_A_Removed_Student_Back_Revives_The_Membership_They_Had()
    {
        var (svc, ctx, uow) = CreateService();
        using var _ = ctx;
        var dto = new CreateCourseMembershipDTO
        {
            CourseId = Ulid.NewUlid(),
            StudentId = Ulid.NewUlid()
        };

        var originalId = await svc.CreateMembershipAsync(dto);
        await uow.CommitAsync();

        await svc.SoftDeleteMembershipAsync(originalId);
        await uow.CommitAsync();

        var revivedId = await svc.CreateMembershipAsync(dto);
        await uow.CommitAsync();

        // The same row comes back to life rather than a second one being laid on top,
        // which is what the unique index would refuse anyway.
        Assert.Equal(originalId, revivedId);
        Assert.Single(ctx.GetDbSet<CourseMembershipModel>());
        Assert.True(await svc.HasAccessAsync(dto.CourseId, dto.StudentId));
    }

    [Fact]
    public async Task Create_Get_Search_SoftDelete_Membership_Flow()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var mapper = CreateMapper();
        var current = MakeUser(UserRoles.Admin);
        var courseMembershipRepo = new CourseMembershipRepository(ctx);
        var service = new CourseMembershipService(
            courseMembershipRepo,
            mapper,
            current,
            new Mock<IEventPublisher>().Object
        );

        var courseId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();
        var id = await service.CreateMembershipAsync(new CreateCourseMembershipDTO
        {
            CourseId = courseId,
            StudentId = studentId
        });
        await uow.CommitAsync();
        Assert.NotEqual(default, id);

        var fetched = await service.GetMembershipByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal(courseId, fetched!.CourseId);
        Assert.Equal(studentId, fetched.StudentId);
        Assert.False(fetched.IsArchived);

        var search = await service.GetMembershipsAsync(new CourseMembershipFilter { Page = 1, PerPage = 10 });
        Assert.Equal(1, search.Total);

        Assert.True(await service.HasAccessAsync(courseId, studentId));

        await service.SoftDeleteMembershipAsync(id);
        await uow.CommitAsync();
        var after = await service.GetMembershipByIdAsync(id);
        Assert.False(after!.IsActive);
    }
}
