using Moq;
using Noo.Api.AssignedWorks;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Courses.Services;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Works.Models;
using Noo.Api.Works.Services;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkServiceTests
{
    private sealed class CapturingPublisher : IEventPublisher
    {
        public List<IDomainEvent> Published { get; } = new();

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : IDomainEvent
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }
    }

    private static (AssignedWorkService svc, NooDbContext ctx, Mock<IUnitOfWork> uowMock, Mock<ICurrentUser> currentUserMock, CapturingPublisher publisher) CreateService(UserRoles role, Ulid? userId = null)
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uowMock = TestHelpers.CreateUowMock(ctx);
        var currentUser = new Mock<ICurrentUser>();
        userId ??= Ulid.NewUlid();
        currentUser.SetupGet(c => c.UserId).Returns(userId);
        currentUser.SetupGet(c => c.UserRole).Returns(role);
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.IsInRole(It.IsAny<UserRoles[]>())).Returns<UserRoles[]>(r => r.Contains(role));
        var publisher = new CapturingPublisher();
        var mapperCfg = MapperTestUtils.CreateMapperConfig(cfg => cfg.AddProfile(new AssignedWorkMapperProfile()));
        var mapper = mapperCfg.CreateMapper();
        var assignedWorkRepo = new AssignedWorkRepository(ctx);
        var assignedWorkAnswerRepo = new AssignedWorkAnswerRepository(ctx);
        var assignedWorkCommentRepo = new AssignedWorkCommentRepository(ctx);
        var courseWorkAssignmentRepo = new Mock<ICourseWorkAssignmentRepository>();
        var mentorAssignmentRepo = new Mock<IMentorAssignmentRepository>();
        var workTaskRepo = new WorkTaskRepository(ctx);
        var svc = new AssignedWorkService(
            assignedWorkRepo,
            assignedWorkAnswerRepo,
            assignedWorkCommentRepo,
            courseWorkAssignmentRepo.Object,
            mentorAssignmentRepo.Object,
            currentUser.Object,
            workTaskRepo,
            mapper,
            publisher,
            new MemoryCacheRepository()
        );
        return (svc, ctx, uowMock, currentUser, publisher);
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
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        var newHelper = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(newHelper);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await svc.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = newHelper.Id });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newHelper.Id, updated!.HelperMentorId);
    }

    [Fact]
    public async Task AddHelperMentor_NoOp_When_Already_Main()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await svc.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = mainMentor.Id });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Null(updated!.HelperMentorId);
    }

    [Fact]
    public async Task MarkAsSolved_Sets_Fields_And_Publishes_Event()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid());

        await svc.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.Solved, updated!.SolveStatus);
        Assert.NotNull(updated.SolvedAt);

        var solved = Assert.Single(publisher.Published.OfType<AssignedWorkSolvedEvent>());
        Assert.Equal(aw.Id, solved.AssignedWorkId);
        Assert.Equal(student.Id, solved.StudentId);
    }

    [Fact]
    public async Task MarkAsSolved_AlreadySolved_Throws()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.Solved);
        aw.SolvedAt = DateTime.UtcNow; ctx.SaveChanges();

        await Assert.ThrowsAsync<AssignedWorkAlreadySolvedException>(() => svc.MarkAsSolvedAsync(aw.Id));
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task MarkAsChecked_Sets_Fields()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.Solved);
        aw.SolvedAt = DateTime.UtcNow; ctx.SaveChanges();

        await svc.MarkAsCheckedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.Checked, updated!.CheckStatus);
        Assert.NotNull(updated.CheckedAt);
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
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.Solved);
        aw.SolvedAt = DateTime.UtcNow; ctx.SaveChanges();

        await svc.ReturnToSolveAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.InProgress, updated!.SolveStatus);
        Assert.Null(updated.SolvedAt);
    }

    [Fact]
    public async Task ReturnToCheck_Resets_Check_State()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.Solved, checkStatus: AssignedWorkCheckStatus.Checked);
        aw.SolvedAt = DateTime.UtcNow; aw.CheckedAt = DateTime.UtcNow; ctx.SaveChanges();

        await svc.ReturnToCheckAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.NotChecked, updated!.CheckStatus);
        Assert.Null(updated.CheckedAt);
    }

    [Fact]
    public async Task Remake_Creates_New_Attempt_With_Excluded_Correct_Tasks_When_Option_Set()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), type: WorkType.Test, solveStatus: AssignedWorkSolveStatus.Solved, checkStatus: AssignedWorkCheckStatus.Checked);
        aw.SolvedAt = DateTime.UtcNow; aw.CheckedAt = DateTime.UtcNow; ctx.SaveChanges();

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
        ctx.SaveChanges();
        Assert.NotEqual(default, newId);
        var all = ctx.GetDbSet<AssignedWorkModel>().ToList();
        Assert.Equal(2, all.Count);
        var copy = all.Single(x => x.Id == newId);
        Assert.True(copy.Attempt == aw.Attempt + 1);
        Assert.NotNull(copy.ExcludedTaskIds);
        Assert.Contains(task1.Id, copy.ExcludedTaskIds!);
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
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var newDeadline = aw.SolveDeadlineAt!.Value.Add(AssignedWorkConfig.MaxSolveDeadlineShift).AddMinutes(-1);
        await svc.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline, NotifyOthers = true });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newDeadline, updated!.SolveDeadlineAt);
    }

    [Fact]
    public async Task ShiftDeadline_Mentor_Within_Limit_Succeeds()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id);
        var newDeadline = aw.CheckDeadlineAt!.Value.Add(AssignedWorkConfig.MaxCheckDeadlineShift).AddMinutes(-1);
        await svc.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline });
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newDeadline, updated!.CheckDeadlineAt);
    }

    [Fact]
    public async Task Delete_AssignedWork_When_Not_Solved_Removes()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.NotSolved);
        await svc.DeleteAsync(aw.Id);
        await ctx.SaveChangesAsync();
        var exists = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Null(exists);
    }

    [Fact]
    public async Task SaveAnswer_Inserts_New_Answer_When_No_Id()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var answerDto = new UpsertAssignedWorkAnswerDTO { TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 5 };

        var id = await svc.SaveAnswerAsync(aw.Id, answerDto);
        await ctx.SaveChangesAsync();

        Assert.NotEqual(default, id);
        var saved = await ctx.GetDbSet<AssignedWorkAnswerModel>().FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(aw.Id, saved!.AssignedWorkId);
        Assert.Equal(5, saved.Score);
    }

    [Fact]
    public async Task SaveAnswer_Updates_Existing_Answer_When_Id_Provided()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var existing = new AssignedWorkAnswerModel
        {
            AssignedWorkId = aw.Id,
            TaskId = Ulid.NewUlid(),
            Status = AssignedWorkAnswerStatus.NotSubmitted,
            MaxScore = 10,
            Score = 1,
            WordContent = "old",
        };
        ctx.GetDbSet<AssignedWorkAnswerModel>().Add(existing);
        ctx.SaveChanges();

        var dto = new UpsertAssignedWorkAnswerDTO
        {
            Id = existing.Id,
            TaskId = existing.TaskId,
            Status = AssignedWorkAnswerStatus.Submitted,
            MaxScore = 10,
            Score = 8,
            WordContent = "new",
        };

        var id = await svc.SaveAnswerAsync(aw.Id, dto);
        await ctx.SaveChangesAsync();

        Assert.Equal(existing.Id, id);
        var all = ctx.GetDbSet<AssignedWorkAnswerModel>().Where(a => a.AssignedWorkId == aw.Id).ToList();
        Assert.Single(all);
        Assert.Equal(8, all[0].Score);
        Assert.Equal("new", all[0].WordContent);
        Assert.Equal(AssignedWorkAnswerStatus.Submitted, all[0].Status);
        Assert.Equal(aw.Id, all[0].AssignedWorkId);
    }

    [Fact]
    public async Task SaveAnswer_Throws_NotFound_When_Id_Does_Not_Exist()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var dto = new UpsertAssignedWorkAnswerDTO
        {
            Id = Ulid.NewUlid(),
            TaskId = Ulid.NewUlid(),
            Status = AssignedWorkAnswerStatus.Submitted,
            MaxScore = 10,
            Score = 5,
        };

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.SaveAnswerAsync(aw.Id, dto));
    }

    [Fact]
    public async Task SaveAnswer_Throws_NotFound_When_Answer_Belongs_To_Different_AssignedWork()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var ownAw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var otherAw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var otherAnswer = new AssignedWorkAnswerModel
        {
            AssignedWorkId = otherAw.Id,
            TaskId = Ulid.NewUlid(),
            Status = AssignedWorkAnswerStatus.Submitted,
            MaxScore = 10,
            Score = 5,
        };
        ctx.GetDbSet<AssignedWorkAnswerModel>().Add(otherAnswer);
        ctx.SaveChanges();

        var dto = new UpsertAssignedWorkAnswerDTO
        {
            Id = otherAnswer.Id,
            TaskId = otherAnswer.TaskId,
            Status = AssignedWorkAnswerStatus.Submitted,
            MaxScore = 10,
            Score = 9,
        };

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.SaveAnswerAsync(ownAw.Id, dto));
    }

    [Fact]
    public async Task SaveComment_Assumes_Behavior_Returns_Id()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var commentDto = new UpsertAssignedWorkCommentDTO();
        var id = svc.SaveComment(aw.Id, commentDto);
        Assert.NotEqual(default, id);
    }
}
