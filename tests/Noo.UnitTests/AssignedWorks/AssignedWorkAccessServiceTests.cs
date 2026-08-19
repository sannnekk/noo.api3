using Moq;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Security.Authorization;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkAccessServiceTests
{
    private static (AssignedWorkAccessService svc, Ulid userId) Create(UserRoles role)
    {
        var userId = Ulid.NewUlid();
        var user = new Mock<ICurrentUser>();
        user.SetupGet(x => x.UserId).Returns(userId);
        user.SetupGet(x => x.UserRole).Returns(role);
        user.SetupGet(x => x.IsAuthenticated).Returns(true);

        return (new AssignedWorkAccessService(user.Object), userId);
    }

    private static AssignedWorkModel Work(
        Ulid student,
        Ulid mainMentor,
        Ulid? helperMentor = null,
        AssignedWorkSolveStatus solve = AssignedWorkSolveStatus.NotSolved,
        AssignedWorkCheckStatus check = AssignedWorkCheckStatus.NotChecked
    ) => new()
    {
        Title = "A",
        Type = Noo.Api.Works.Types.WorkType.Test,
        Attempt = 1,
        StudentId = student,
        MainMentorId = mainMentor,
        HelperMentorId = helperMentor,
        SolveStatus = solve,
        CheckStatus = check,
        CheckedAt = AssignedWorkStatuses.Checked.Contains(check) ? Noo.Api.Core.Utils.Clock.Now : null,
        MaxScore = 10
    };

    [Fact]
    public void Student_Reads_Own_Work_Only()
    {
        var (svc, studentId) = Create(UserRoles.Student);

        Assert.True(svc.CanRead(Work(studentId, Ulid.NewUlid())));
        Assert.False(svc.CanRead(Work(Ulid.NewUlid(), Ulid.NewUlid())));
    }

    [Fact]
    public void Mentor_Reads_Work_They_Are_On_As_Either_Mentor()
    {
        var (svc, mentorId) = Create(UserRoles.Mentor);

        Assert.True(svc.CanRead(Work(Ulid.NewUlid(), mentorId)));
        Assert.True(svc.CanRead(Work(Ulid.NewUlid(), Ulid.NewUlid(), helperMentor: mentorId)));
        Assert.False(svc.CanRead(Work(Ulid.NewUlid(), Ulid.NewUlid())));
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Teacher)]
    [InlineData(UserRoles.Assistant)]
    public void Staff_Read_Any_Work(UserRoles role)
    {
        var (svc, _) = Create(role);

        Assert.True(svc.CanRead(Work(Ulid.NewUlid(), Ulid.NewUlid())));
    }

    [Fact]
    public void Student_Deletes_Own_Work_Only_Before_Handing_It_In()
    {
        var (svc, studentId) = Create(UserRoles.Student);

        Assert.True(svc.CanDelete(Work(studentId, Ulid.NewUlid())));
        Assert.False(
            svc.CanDelete(
                Work(studentId, Ulid.NewUlid(), solve: AssignedWorkSolveStatus.SolvedInDeadline)
            )
        );
        Assert.False(svc.CanDelete(Work(Ulid.NewUlid(), Ulid.NewUlid())));
    }

    [Fact]
    public void Mentor_Never_Deletes()
    {
        var (svc, mentorId) = Create(UserRoles.Mentor);

        Assert.False(svc.CanDelete(Work(Ulid.NewUlid(), mentorId)));
    }

    [Fact]
    public void Mentors_Are_Assigned_Only_While_The_Work_Is_Unchecked()
    {
        var (admin, _) = Create(UserRoles.Admin);
        var unchecked_ = Work(Ulid.NewUlid(), Ulid.NewUlid());
        var checked_ = Work(
            Ulid.NewUlid(),
            Ulid.NewUlid(),
            check: AssignedWorkCheckStatus.CheckedInDeadline
        );

        Assert.True(admin.CanAssignMainMentor(unchecked_));
        Assert.True(admin.CanAssignHelperMentor(unchecked_));
        Assert.False(admin.CanAssignMainMentor(checked_));
        Assert.False(admin.CanAssignHelperMentor(checked_));
    }

    [Fact]
    public void Mentor_Brings_In_A_Helper_Only_On_Their_Own_Work_And_Never_Replaces_The_Main_One()
    {
        var (svc, mentorId) = Create(UserRoles.Mentor);

        Assert.True(svc.CanAssignHelperMentor(Work(Ulid.NewUlid(), mentorId)));
        Assert.False(svc.CanAssignHelperMentor(Work(Ulid.NewUlid(), Ulid.NewUlid())));
        Assert.False(svc.CanAssignMainMentor(Work(Ulid.NewUlid(), mentorId)));
    }

    [Fact]
    public void Assistant_Replaces_The_Main_Mentor_But_Does_Not_Add_Helpers()
    {
        var (svc, _) = Create(UserRoles.Assistant);
        var work = Work(Ulid.NewUlid(), Ulid.NewUlid());

        Assert.True(svc.CanAssignMainMentor(work));
        Assert.False(svc.CanAssignHelperMentor(work));
    }
}
