using AutoMapper;
using Moq;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
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

    [Fact]
    public async Task Create_Get_Search_SoftDelete_Membership_Flow()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var mapper = CreateMapper();
        var current = MakeUser(UserRoles.Admin);
        var courseMembershipRepo = new CourseMembershipRepository(ctx);
        var service = new CourseMembershipService(uow, courseMembershipRepo, mapper, current);

        var courseId = Ulid.NewUlid();
        var studentId = Ulid.NewUlid();
        var id = await service.CreateMembershipAsync(new CreateCourseMembershipDTO
        {
            CourseId = courseId,
            StudentId = studentId
        });
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
        var after = await service.GetMembershipByIdAsync(id);
        Assert.False(after!.IsActive);
    }
}
