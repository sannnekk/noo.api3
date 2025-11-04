using Moq;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkAccessServiceTests
{
    private static (AssignedWorkAccessService svc, NooDbContext ctx, Mock<IUnitOfWork> uowMock, Mock<ICurrentUser> user) Create(UserRoles role)
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx);
        var user = new Mock<ICurrentUser>();
        user.SetupGet(x => x.UserId).Returns(Ulid.NewUlid());
        user.SetupGet(x => x.UserRole).Returns(role);
        user.SetupGet(x => x.IsAuthenticated).Returns(true);
        var svc = new AssignedWorkAccessService(uow.Object, user.Object);
        return (svc, ctx, uow, user);
    }

    private static AssignedWorkModel Seed(NooDbContext ctx, Ulid student, Ulid mentor, AssignedWorkSolveStatus solve = AssignedWorkSolveStatus.NotSolved, AssignedWorkCheckStatus check = AssignedWorkCheckStatus.NotChecked)
    {
        var aw = new AssignedWorkModel
        {
            Title = "A",
            Type = Noo.Api.Works.Types.WorkType.Test,
            Attempt = 1,
            StudentId = student,
            MainMentorId = mentor,
            SolveStatus = solve,
            CheckStatus = check,
            MaxScore = 10
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        ctx.SaveChanges();
        return aw;
    }

    [Fact]
    public async Task Student_Can_Get_And_Save_Own_InProgress()
    {
        var (svc, ctx, _, user) = Create(UserRoles.Student);
        var sid = user.Object.UserId!.Value;
        var aw = Seed(ctx, sid, Ulid.NewUlid(), solve: AssignedWorkSolveStatus.InProgress);
        Assert.True(await svc.CanGetAssignedWorkAsync(aw.Id));
        Assert.True(await svc.CanSaveAssignedWorkAsync(aw.Id));
    }

    [Fact]
    public async Task Student_Cannot_Delete_Solved()
    {
        var (svc, ctx, _, user) = Create(UserRoles.Student);
        var sid = user.Object.UserId!.Value;
        var aw = Seed(ctx, sid, Ulid.NewUlid(), solve: AssignedWorkSolveStatus.Solved);
        Assert.False(await svc.CanDeleteAssignedWorkAsync(aw.Id));
    }

    [Fact]
    public async Task Mentor_Can_Save_When_NotChecked()
    {
        var (svc, ctx, _, user) = Create(UserRoles.Mentor);
        var mid = user.Object.UserId!.Value;
        var aw = Seed(ctx, Ulid.NewUlid(), mid, solve: AssignedWorkSolveStatus.Solved, check: AssignedWorkCheckStatus.InProgress);
        Assert.True(await svc.CanSaveAssignedWorkAsync(aw.Id));
    }

    [Fact]
    public async Task Mentor_Cannot_Save_When_Checked()
    {
        var (svc, ctx, _, user) = Create(UserRoles.Mentor);
        var mid = user.Object.UserId!.Value;
        var aw = Seed(ctx, Ulid.NewUlid(), mid, solve: AssignedWorkSolveStatus.Solved, check: AssignedWorkCheckStatus.Checked);
        Assert.False(await svc.CanSaveAssignedWorkAsync(aw.Id));
    }

    [Fact]
    public async Task Admin_Can_Add_Helper_And_Main()
    {
        var (svc, ctx, _, user) = Create(UserRoles.Admin);
        var aw = Seed(ctx, Ulid.NewUlid(), Ulid.NewUlid());
        Assert.True(await svc.CanAddHelperMentorAsync(aw.Id));
        Assert.True(await svc.CanAddMainMentorAsync(aw.Id));
    }
}
