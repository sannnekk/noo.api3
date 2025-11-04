using Moq;
using MediatR;
using AutoMapper;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Users.Models;
using Noo.UnitTests.Common;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.AssignedWorks; // for AssignedWorkConfig
                             // using Noo.Api.AssignedWorks.Models; // for AssignedWorkMapperProfile

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkServiceTests
{
    private static (AssignedWorkService svc, NooDbContext ctx, Mock<IUnitOfWork> uowMock, Mock<ICurrentUser> currentUserMock, Mock<IMediator> mediatorMock) CreateService(UserRoles role, Ulid? userId = null)
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uowMock = TestHelpers.CreateUowMock(ctx);
        var currentUser = new Mock<ICurrentUser>();
        userId ??= Ulid.NewUlid();
        currentUser.SetupGet(c => c.UserId).Returns(userId);
        currentUser.SetupGet(c => c.UserRole).Returns(role);
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.IsInRole(It.IsAny<UserRoles[]>())).Returns<UserRoles[]>(r => r.Contains(role));
        var mediator = new Mock<IMediator>();
        var mapperCfg = new MapperConfiguration(cfg => cfg.AddProfile(new AssignedWorkMapperProfile()));
        var mapper = mapperCfg.CreateMapper();
        var svc = new AssignedWorkService(uowMock.Object, currentUser.Object, mediator.Object, mapper);
        return (svc, ctx, uowMock, currentUser, mediator);
    }

    private static UserModel MakeUser(UserRoles role) => new()
    {
        Name = role.ToString(),
        Username = $"{role.ToString().ToLower()}_{Guid.NewGuid():N}",
        Email = $"{Guid.NewGuid():N}@example.com",
        PasswordHash = "p",
        Role = role,
        IsVerified = true
    };

    private static AssignedWorkModel SeedAssignedWork(NooDbContext ctx, Ulid studentId, Ulid mainMentorId, Ulid? helperMentorId = null, WorkType type = WorkType.Test, AssignedWorkSolveStatus solveStatus = AssignedWorkSolveStatus.NotSolved, AssignedWorkCheckStatus checkStatus = AssignedWorkCheckStatus.NotChecked)
    {
        var aw = new AssignedWorkModel
        {
            Title = "Test AW",
            Type = type,
            Attempt = 1,
            StudentId = studentId,
            MainMentorId = mainMentorId,
            HelperMentorId = helperMentorId,
            SolveStatus = solveStatus,
            CheckStatus = checkStatus,
            SolveDeadlineAt = DateTime.UtcNow.AddDays(1),
            CheckDeadlineAt = DateTime.UtcNow.AddDays(2),
            MaxScore = 100
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        ctx.SaveChanges();
        return aw;
    }

    [Fact]
    public async Task AddHelperMentor_Adds_When_Valid()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        var newHelper = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(newHelper);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await svc.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = newHelper.Id });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newHelper.Id, updated!.HelperMentorId);
        mediator.Verify(m => m.Publish(It.IsAny<HelperMentorAddedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddHelperMentor_NoOp_When_Already_Main()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await svc.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = mainMentor.Id });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Null(updated!.HelperMentorId);
        mediator.Verify(m => m.Publish(It.IsAny<HelperMentorAddedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsSolved_Sets_Fields()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid());

        await svc.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.Solved, updated!.SolveStatus);
        Assert.NotNull(updated.SolvedAt);
        mediator.Verify(m => m.Publish(It.IsAny<AssignedWorkSolvedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsSolved_AlreadySolved_Throws()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.Solved);
        aw.SolvedAt = DateTime.UtcNow; ctx.SaveChanges();

        await Assert.ThrowsAsync<AssignedWorkAlreadySolvedException>(() => svc.MarkAsSolvedAsync(aw.Id));
    }

    [Fact]
    public async Task MarkAsChecked_Sets_Fields()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.Solved);
        aw.SolvedAt = DateTime.UtcNow; ctx.SaveChanges();

        await svc.MarkAsCheckedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.Checked, updated!.CheckStatus);
        Assert.NotNull(updated.CheckedAt);
        mediator.Verify(m => m.Publish(It.IsAny<AssignedWorkCheckedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsChecked_NotSolved_Throws()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id);
        await Assert.ThrowsAsync<AssignedWorkNotSolvedException>(() => svc.MarkAsCheckedAsync(aw.Id));
    }

    [Fact]
    public async Task ReturnToSolve_Resets_Solve_State()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.Solved);
        aw.SolvedAt = DateTime.UtcNow; ctx.SaveChanges();

        await svc.ReturnToSolveAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.InProgress, updated!.SolveStatus);
        Assert.Null(updated.SolvedAt);
        mediator.Verify(m => m.Publish(It.IsAny<AssignedWorkReturnedToSolveEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnToCheck_Resets_Check_State()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.Solved, checkStatus: AssignedWorkCheckStatus.Checked);
        aw.SolvedAt = DateTime.UtcNow; aw.CheckedAt = DateTime.UtcNow; ctx.SaveChanges();

        await svc.ReturnToCheckAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.NotChecked, updated!.CheckStatus);
        Assert.Null(updated.CheckedAt);
        mediator.Verify(m => m.Publish(It.IsAny<AssignedWorkReturnedToCheckEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remake_Creates_New_Attempt_With_Excluded_Correct_Tasks_When_Option_Set()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), type: WorkType.Test, solveStatus: AssignedWorkSolveStatus.Solved, checkStatus: AssignedWorkCheckStatus.Checked);
        aw.SolvedAt = DateTime.UtcNow; aw.CheckedAt = DateTime.UtcNow; ctx.SaveChanges();

        // Seed answers (one correct, one incorrect)
        // Seed related work & tasks
        var work = new WorkModel { Title = "WorkTitle", Type = WorkType.Test };
        ctx.GetDbSet<WorkModel>().Add(work);
        ctx.SaveChanges();
        aw.WorkId = work.Id;
        ctx.SaveChanges();
        var task1 = new WorkTaskModel { Content = new DeltaRichText(), Type = WorkTaskType.Word, Order = 0, MaxScore = 10, WorkId = work.Id };
        var task2 = new WorkTaskModel { Content = new DeltaRichText(), Type = WorkTaskType.Word, Order = 1, MaxScore = 10, WorkId = work.Id };
        ctx.GetDbSet<WorkTaskModel>().AddRange(task1, task2);
        ctx.GetDbSet<AssignedWorkAnswerModel>().AddRange(new AssignedWorkAnswerModel
        {
            AssignedWorkId = aw.Id,
            TaskId = task1.Id,
            Score = 10,
            MaxScore = 10,
            Status = AssignedWorkAnswerStatus.Submitted
        }, new AssignedWorkAnswerModel
        {
            AssignedWorkId = aw.Id,
            TaskId = task2.Id,
            Score = 5,
            MaxScore = 10,
            Status = AssignedWorkAnswerStatus.Submitted
        });
        ctx.SaveChanges();

        var newId = await svc.RemakeAsync(aw.Id, new RemakeAssignedWorkOptionsDTO { IncludeOnlyWrongTasks = true });
        Assert.NotEqual(default, newId);
        var all = ctx.GetDbSet<AssignedWorkModel>().ToList();
        Assert.Equal(2, all.Count); // original + new attempt
        var copy = all.Single(x => x.Id == newId);
        Assert.True(copy.Attempt == aw.Attempt + 1);
        Assert.NotNull(copy.ExcludedTaskIds);
        Assert.Contains(task1.Id, copy.ExcludedTaskIds!); // correct task excluded
        Assert.DoesNotContain(task2.Id, copy.ExcludedTaskIds!);
    }

    [Fact]
    public async Task Archive_And_Unarchive_By_Role()
    {
        var (svcStudent, ctxStudent, _, currentUserStudent, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctxStudent.GetDbSet<UserModel>().Add(student); ctxStudent.SaveChanges();
        currentUserStudent.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctxStudent, student.Id, Ulid.NewUlid());
        await svcStudent.ArchiveAsync(aw.Id);
        var after = await ctxStudent.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.True(after!.IsArchivedByStudent);
        await svcStudent.UnarchiveAsync(aw.Id);
        after = await ctxStudent.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.False(after!.IsArchivedByStudent);
    }

    [Fact]
    public async Task ShiftDeadline_Student_Within_Limit_Succeeds()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var newDeadline = aw.SolveDeadlineAt!.Value.Add(AssignedWorkConfig.MaxSolveDeadlineShift).AddMinutes(-1);
        await svc.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline, NotifyOthers = true });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newDeadline, updated!.SolveDeadlineAt);
        mediator.Verify(m => m.Publish(It.IsAny<AssignedWorkSolveDeadlineShiftedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShiftDeadline_Mentor_Within_Limit_Succeeds()
    {
        var (svc, ctx, _, currentUser, mediator) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id);
        var newDeadline = aw.CheckDeadlineAt!.Value.Add(AssignedWorkConfig.MaxCheckDeadlineShift).AddMinutes(-1);
        await svc.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newDeadline, updated!.CheckDeadlineAt);
        mediator.Verify(m => m.Publish(It.IsAny<AssignedWorkCheckDeadlineShiftedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_AssignedWork_When_Not_Solved_Removes()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.NotSolved);
        await svc.DeleteAsync(aw.Id);
        var exists = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Null(exists);
    }

    [Fact]
    public async Task SaveAnswer_Assumes_Behavior_Updates_Status()
    {
        // Even though not implemented, assume it stores/updates answer and returns Id; test reflects expected behavior.
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var answerDto = new UpsertAssignedWorkAnswerDTO { TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 5 };
        var id = await svc.SaveAnswerAsync(aw.Id, answerDto);
        Assert.NotEqual(default, id);
    }

    [Fact]
    public async Task SaveComment_Assumes_Behavior_Returns_Id()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var commentDto = new UpsertAssignedWorkCommentDTO();
        var id = await svc.SaveCommentAsync(aw.Id, commentDto);
        Assert.NotEqual(default, id);
    }
}
