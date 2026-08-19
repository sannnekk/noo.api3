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
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Utils.Richtext.Delta;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using Noo.Api.Works.Models;
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

    /// <summary>
    /// The module's four services over one in-memory database, so a test can drive a work
    /// through whichever of them the step belongs to.
    /// </summary>
    private sealed record Services(
        AssignedWorkService Work,
        AssignedWorkLifecycleService Lifecycle,
        AssignedWorkEditingService Editing,
        AssignedWorkMentorService Mentors
    );

    private static (Services svc, NooDbContext ctx, Mock<IUnitOfWork> uowMock, Mock<ICurrentUser> currentUserMock, CapturingPublisher publisher) CreateService(UserRoles role, Ulid? userId = null)
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var uowMock = TestHelpers.CreateUowMock(ctx);
        var currentUser = new Mock<ICurrentUser>
        {
            // Run ICurrentUser's default interface methods (RequireUserId/RequireUserRole)
            // against the mocked properties instead of returning stubbed defaults.
            CallBase = true
        };
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
        var userRepo = new UserRepository(ctx);
        var access = new AssignedWorkAccessService(currentUser.Object);
        var svc = new Services(
            new AssignedWorkService(
                assignedWorkRepo,
                assignedWorkAnswerRepo,
                courseWorkAssignmentRepo.Object,
                mentorAssignmentRepo.Object,
                access,
                currentUser.Object,
                publisher,
                new MemoryCacheRepository()
            ),
            new AssignedWorkLifecycleService(
                assignedWorkRepo,
                new TaskCheckService(),
                currentUser.Object,
                publisher
            ),
            new AssignedWorkEditingService(
                assignedWorkRepo,
                assignedWorkAnswerRepo,
                assignedWorkCommentRepo,
                currentUser.Object,
                mapper,
                publisher
            ),
            new AssignedWorkMentorService(
                assignedWorkRepo,
                userRepo,
                access,
                currentUser.Object,
                publisher
            )
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

    private static AssignedWorkModel SeedAssignedWork(NooDbContext ctx, Ulid studentId, Ulid mainMentorId, Ulid? helperMentorId = null, WorkType type = WorkType.Test, AssignedWorkSolveStatus solveStatus = AssignedWorkSolveStatus.NotSolved, AssignedWorkCheckStatus checkStatus = AssignedWorkCheckStatus.NotChecked, DateTime? solveDeadlineAt = null, DateTime? checkDeadlineAt = null)
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
            SolveDeadlineAt = solveDeadlineAt ?? Clock.Now.AddDays(1),
            CheckDeadlineAt = checkDeadlineAt ?? Clock.Now.AddDays(2),
            MaxScore = 100
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        ctx.SaveChanges();
        return aw;
    }

    private static List<WorkTaskModel> SeedWorkWithAnswers(NooDbContext ctx, AssignedWorkModel aw, params WorkTaskType[] taskTypes)
    {
        var work = new WorkModel { Title = "WorkTitle", Type = aw.Type };
        ctx.GetDbSet<WorkModel>().Add(work);
        ctx.SaveChanges();

        aw.WorkId = work.Id;

        var tasks = taskTypes
            .Select((type, order) => new WorkTaskModel
            {
                Content = new DeltaRichText(),
                Type = type,
                CheckStrategy = WorkTaskCheckStrategy.ExactMatchOrZero,
                RightAnswers = ["answer"],
                Order = order,
                MaxScore = 10,
                WorkId = work.Id
            })
            .ToList();
        ctx.GetDbSet<WorkTaskModel>().AddRange(tasks);

        ctx.GetDbSet<AssignedWorkAnswerModel>().AddRange(tasks.Select(task => new AssignedWorkAnswerModel
        {
            AssignedWorkId = aw.Id,
            TaskId = task.Id,
            WordContent = "answer",
            MaxScore = 10,
            Status = AssignedWorkAnswerStatus.NotSubmitted
        }));
        ctx.SaveChanges();

        return tasks;
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

        await svc.Mentors.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = newHelper.Id });
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

        await svc.Mentors.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = mainMentor.Id });
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

        await svc.Lifecycle.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.SolvedInDeadline, updated!.SolveStatus);
        Assert.NotNull(updated.SolvedAt);

        var solved = Assert.Single(publisher.Published.OfType<SolvedEvent>());
        Assert.Equal(aw.Id, solved.AssignedWorkId);
        Assert.Equal(student.Id, solved.StudentId);
    }

    [Fact]
    public async Task MarkAsSolved_Past_Deadline_Sets_SolvedAfterDeadline()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid(), solveDeadlineAt: Clock.Now.AddDays(-1));

        await svc.Lifecycle.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.SolvedAfterDeadline, updated!.SolveStatus);
    }

    [Fact]
    public async Task MarkAsSolved_Checks_Work_Automatically_When_Every_Task_Is_Automatic()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid());
        SeedWorkWithAnswers(ctx, aw, WorkTaskType.Word, WorkTaskType.Word);

        await svc.Lifecycle.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.CheckedAutomatically, updated!.CheckStatus);
        Assert.NotNull(updated.CheckedAt);

        var checkedEvent = Assert.Single(publisher.Published.OfType<CheckedEvent>());
        Assert.Null(checkedEvent.MentorId);
    }

    [Fact]
    public async Task MarkAsSolved_Leaves_Work_Unchecked_When_A_Task_Needs_A_Mentor()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid());
        SeedWorkWithAnswers(ctx, aw, WorkTaskType.Word, WorkTaskType.Essay);

        await svc.Lifecycle.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.NotChecked, updated!.CheckStatus);
        Assert.Null(updated.CheckedAt);
        Assert.Empty(publisher.Published.OfType<CheckedEvent>());
    }

    [Fact]
    public async Task MarkAsSolved_Ignores_Excluded_Tasks_When_Deciding_On_Automatic_Check()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid());
        var tasks = SeedWorkWithAnswers(ctx, aw, WorkTaskType.Word, WorkTaskType.Essay);
        aw.ExcludedTaskIds = [tasks[1].Id]; ctx.SaveChanges();

        await svc.Lifecycle.MarkAsSolvedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.CheckedAutomatically, updated!.CheckStatus);
    }

    [Fact]
    public async Task MarkAsSolved_AlreadySolved_Throws()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now; ctx.SaveChanges();

        await Assert.ThrowsAsync<AssignedWorkAlreadySolvedException>(() => svc.Lifecycle.MarkAsSolvedAsync(aw.Id));
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task MarkAsChecked_Sets_Fields()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.MarkAsCheckedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.CheckedInDeadline, updated!.CheckStatus);
        Assert.NotNull(updated.CheckedAt);
    }

    [Fact]
    public async Task MarkAsChecked_Past_Deadline_Sets_CheckedAfterDeadline()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline, checkDeadlineAt: Clock.Now.AddDays(-1));
        aw.SolvedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.MarkAsCheckedAsync(aw.Id);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.CheckedAfterDeadline, updated!.CheckStatus);
    }

    [Fact]
    public async Task MarkAsChecked_NotSolved_Throws()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id);
        await Assert.ThrowsAsync<AssignedWorkNotSolvedException>(() => svc.Lifecycle.MarkAsCheckedAsync(aw.Id));
    }

    [Fact]
    public async Task AddHelperMentor_Throws_When_Mentor_Does_Not_Exist()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.Mentors.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = Ulid.NewUlid() }));
    }

    [Fact]
    public async Task AddHelperMentor_Throws_When_Target_Is_Not_A_Mentor()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        var notAMentor = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(notAMentor);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.Mentors.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = notAMentor.Id }));
    }

    [Fact]
    public async Task MarkAsSolved_Marks_All_Answers_As_Submitted()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentorId: Ulid.NewUlid());
        ctx.GetDbSet<AssignedWorkAnswerModel>().AddRange(
            new AssignedWorkAnswerModel { AssignedWorkId = aw.Id, TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.NotSubmitted },
            new AssignedWorkAnswerModel { AssignedWorkId = aw.Id, TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.NotSubmitted });
        ctx.SaveChanges();

        await svc.Lifecycle.MarkAsSolvedAsync(aw.Id);

        var answers = ctx.GetDbSet<AssignedWorkAnswerModel>().Where(a => a.AssignedWorkId == aw.Id).ToList();
        Assert.All(answers, a => Assert.Equal(AssignedWorkAnswerStatus.Submitted, a.Status));
    }

    [Fact]
    public async Task MarkAsChecked_Marks_All_Answers_As_Checked()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now;
        ctx.GetDbSet<AssignedWorkAnswerModel>().AddRange(
            new AssignedWorkAnswerModel { AssignedWorkId = aw.Id, TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted },
            new AssignedWorkAnswerModel { AssignedWorkId = aw.Id, TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted });
        ctx.SaveChanges();

        await svc.Lifecycle.MarkAsCheckedAsync(aw.Id);

        var answers = ctx.GetDbSet<AssignedWorkAnswerModel>().Where(a => a.AssignedWorkId == aw.Id).ToList();
        Assert.All(answers, a => Assert.Equal(AssignedWorkAnswerStatus.Checked, a.Status));
    }

    [Fact]
    public async Task ReturnToSolve_Resets_Solve_State()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.ReturnToSolveAsync(aw.Id);
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
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline, checkStatus: AssignedWorkCheckStatus.CheckedInDeadline);
        aw.SolvedAt = Clock.Now; aw.CheckedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.ReturnToCheckAsync(aw.Id);
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
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), type: WorkType.Test, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline, checkStatus: AssignedWorkCheckStatus.CheckedInDeadline);
        aw.SolvedAt = Clock.Now; aw.CheckedAt = Clock.Now; ctx.SaveChanges();

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

        var newId = await svc.Work.RemakeAsync(aw.Id, new RemakeAssignedWorkOptionsDTO { IncludeOnlyWrongTasks = true });
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
        await svcStudent.Work.ArchiveAsync(aw.Id);
        var after = await ctxStudent.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.True(after!.IsArchivedByStudent);
        await svcStudent.Work.UnarchiveAsync(aw.Id);
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
        await svc.Lifecycle.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline, NotifyOthers = true });
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
        await svc.Lifecycle.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline });
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
        await svc.Work.DeleteAsync(aw.Id);
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

        var id = await svc.Editing.SaveAnswerAsync(aw.Id, answerDto);
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

        var id = await svc.Editing.SaveAnswerAsync(aw.Id, dto);
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

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.Editing.SaveAnswerAsync(aw.Id, dto));
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

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(() => svc.Editing.SaveAnswerAsync(ownAw.Id, dto));
    }


    [Fact]
    public async Task Get_Hides_Mentor_Only_Fields_Of_Answers_Nobody_Submitted()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        ctx.GetDbSet<AssignedWorkAnswerModel>().Add(new AssignedWorkAnswerModel
        {
            AssignedWorkId = aw.Id,
            TaskId = Ulid.NewUlid(),
            Status = AssignedWorkAnswerStatus.NotSubmitted,
            Score = 5,
            MaxScore = 10,
            MentorComment = RichTextFactory.Create("early note"),
            DetailedScore = new Dictionary<string, int> { { "a", 1 } }
        });
        ctx.SaveChanges();

        var fetched = await svc.Work.GetAsync(aw.Id);

        var answer = Assert.Single(fetched!.Answers);
        Assert.Null(answer.Score);
        Assert.Null(answer.DetailedScore);
        Assert.Null(answer.MentorComment);
    }

    [Fact]
    public async Task Get_Returns_Nothing_To_Someone_Who_Is_Not_On_The_Work()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        // Someone else's work: the seeded student owns it, the caller does not.
        currentUser.SetupGet(c => c.UserId).Returns(Ulid.NewUlid());
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());

        Assert.Null(await svc.Work.GetAsync(aw.Id));
    }

    [Theory]
    [InlineData(UserRoles.Admin)]
    [InlineData(UserRoles.Teacher)]
    [InlineData(UserRoles.Assistant)]
    public async Task Get_Lets_Staff_Read_A_Work_They_Are_Not_On(UserRoles role)
    {
        var (svc, ctx, _, _, _) = CreateService(role);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());

        Assert.NotNull(await svc.Work.GetAsync(aw.Id));
    }

    [Fact]
    public async Task Archive_Puts_The_Work_Away_For_Staff_Who_Are_Not_On_It()
    {
        var (svc, ctx, _, _, _) = CreateService(UserRoles.Assistant);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), Ulid.NewUlid());

        await svc.Work.ArchiveAsync(aw.Id);
        await ctx.SaveChangesAsync();

        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.True(updated!.IsArchivedByAssistants);
        // Each side archives out of their own list only.
        Assert.False(updated.IsArchivedByStudent);
        Assert.False(updated.IsArchivedByMentors);
    }

    [Fact]
    public async Task Archive_Is_Refused_To_A_Student_Who_Is_Not_On_The_Work()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        currentUser.SetupGet(c => c.UserId).Returns(Ulid.NewUlid());
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), Ulid.NewUlid());

        await Assert.ThrowsAsync<ForbiddenException>(() => svc.Work.ArchiveAsync(aw.Id));
    }

    [Fact]
    public async Task Delete_Of_A_Handed_In_Work_Reports_The_Conflict_Instead_Of_Passing_Silently()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);

        await Assert.ThrowsAsync<AssignedWorkAlreadySolvedException>(
            () => svc.Work.DeleteAsync(aw.Id)
        );
        Assert.NotNull(await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id));
    }

    [Fact]
    public async Task ReplaceMainMentor_Is_Staff_Work_Even_Though_They_Are_On_No_Work()
    {
        var (svc, ctx, _, _, publisher) = CreateService(UserRoles.Assistant);
        var newMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(newMentor); ctx.SaveChanges();
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), Ulid.NewUlid());

        await svc.Mentors.ReplaceMainMentorAsync(aw.Id, new ReplaceMainMentorOptionsDTO { MentorId = newMentor.Id });
        await ctx.SaveChangesAsync();

        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newMentor.Id, updated!.MainMentorId);
        Assert.Single(publisher.Published.OfType<MainMentorChangedEvent>());
    }

    [Fact]
    public async Task ReplaceMainMentor_Is_Refused_To_A_Mentor()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor);
        var other = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(other);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => svc.Mentors.ReplaceMainMentorAsync(aw.Id, new ReplaceMainMentorOptionsDTO { MentorId = other.Id })
        );
    }

    // Builds a service that exposes the work-assignment repository mock, needed only by CreateAsync.
    private static (AssignedWorkService svc, NooDbContext ctx, Mock<ICourseWorkAssignmentRepository> workAssignmentMock, CapturingPublisher publisher) CreateServiceWithWorkAssignment(UserRoles role, Ulid userId)
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var currentUser = new Mock<ICurrentUser> { CallBase = true };
        currentUser.SetupGet(c => c.UserId).Returns(userId);
        currentUser.SetupGet(c => c.UserRole).Returns(role);
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        var publisher = new CapturingPublisher();
        var workAssignmentMock = new Mock<ICourseWorkAssignmentRepository>();
        var svc = new AssignedWorkService(
            new AssignedWorkRepository(ctx),
            new AssignedWorkAnswerRepository(ctx),
            workAssignmentMock.Object,
            new Mock<IMentorAssignmentRepository>().Object,
            new AssignedWorkAccessService(currentUser.Object),
            currentUser.Object,
            publisher,
            new MemoryCacheRepository()
        );
        return (svc, ctx, workAssignmentMock, publisher);
    }

    [Fact]
    public async Task Create_Publishes_CreatedEvent()
    {
        var studentId = Ulid.NewUlid();
        var (svc, ctx, workAssignmentMock, publisher) = CreateServiceWithWorkAssignment(UserRoles.Student, studentId);
        var work = new WorkModel { Title = "W", Type = WorkType.Test, MaxScore = 50, SubjectId = Ulid.NewUlid() };
        var assignment = new CourseWorkAssignmentModel { WorkId = work.Id, Work = work };
        workAssignmentMock.Setup(r => r.GetWithWorkAsync(It.IsAny<Ulid>())).ReturnsAsync(assignment);

        var newId = await svc.CreateAsync(Ulid.NewUlid());
        await ctx.SaveChangesAsync();

        var created = Assert.Single(publisher.Published.OfType<CreatedEvent>());
        Assert.Equal(newId, created.AssignedWorkId);
    }

    [Fact]
    public async Task SaveAnswer_AsStudent_Publishes_StartedSolving_Only_On_First_Save()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());

        await svc.Editing.SaveAnswerAsync(aw.Id, new UpsertAssignedWorkAnswerDTO { TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 5 });
        await ctx.SaveChangesAsync();

        var started = Assert.Single(publisher.Published.OfType<StartedSolvingEvent>());
        Assert.Equal(aw.Id, started.AssignedWorkId);
        Assert.Equal(student.Id, started.StudentId);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkSolveStatus.InProgress, updated!.SolveStatus);

        // A second save must not re-fire the "started" event.
        await svc.Editing.SaveAnswerAsync(aw.Id, new UpsertAssignedWorkAnswerDTO { TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 7 });
        await ctx.SaveChangesAsync();
        Assert.Single(publisher.Published.OfType<StartedSolvingEvent>());
    }

    [Fact]
    public async Task SaveAnswer_AsMentor_Publishes_StartedChecking_Only_On_First_Save()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now;
        var answer = new AssignedWorkAnswerModel { AssignedWorkId = aw.Id, TaskId = Ulid.NewUlid(), Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 4 };
        ctx.GetDbSet<AssignedWorkAnswerModel>().Add(answer);
        ctx.SaveChanges();

        await svc.Editing.SaveAnswerAsync(aw.Id, new UpsertAssignedWorkAnswerDTO { Id = answer.Id, TaskId = answer.TaskId, Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 9 });
        await ctx.SaveChangesAsync();

        var started = Assert.Single(publisher.Published.OfType<StartedCheckingEvent>());
        Assert.Equal(aw.Id, started.AssignedWorkId);
        Assert.Equal(mentor.Id, started.MentorId);
        // The mentor's edit must not be mistaken for the student starting to solve.
        Assert.Empty(publisher.Published.OfType<StartedSolvingEvent>());
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.InProgress, updated!.CheckStatus);

        // A second comment save must not re-fire the "started" event.
        await svc.Editing.SaveAnswerAsync(aw.Id, new UpsertAssignedWorkAnswerDTO { Id = answer.Id, TaskId = answer.TaskId, Status = AssignedWorkAnswerStatus.Submitted, MaxScore = 10, Score = 8 });
        await ctx.SaveChangesAsync();
        Assert.Single(publisher.Published.OfType<StartedCheckingEvent>());
    }

    [Fact]
    public async Task SaveComment_AsStudent_Creates_Then_Updates_The_Student_Comment()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());

        var id = await svc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("first") });
        await ctx.SaveChangesAsync();

        var created = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(id, created!.StudentCommentId);
        Assert.Null(created.MainMentorCommentId);

        // The same seat writing again must edit the comment in place, not add a second one.
        var secondId = await svc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("second") });
        await ctx.SaveChangesAsync();

        Assert.Equal(id, secondId);
        Assert.Single(ctx.GetDbSet<AssignedWorkCommentModel>());
        var comment = await ctx.GetDbSet<AssignedWorkCommentModel>().FindAsync(id);
        Assert.Equal("second", comment!.Content!.ToString());
    }

    [Fact]
    public async Task SaveComment_Writes_The_Seat_The_Caller_Holds_On_The_Work()
    {
        var studentId = Ulid.NewUlid();
        var mainMentor = Ulid.NewUlid();
        var helperMentor = Ulid.NewUlid();

        var (mainSvc, ctx, _, mainUser, _) = CreateService(UserRoles.Mentor, mainMentor);
        var aw = SeedAssignedWork(ctx, studentId, mainMentor, helperMentor);
        mainUser.SetupGet(c => c.UserId).Returns(mainMentor);

        var mainId = await mainSvc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("main") });
        await ctx.SaveChangesAsync();

        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(mainId, updated!.MainMentorCommentId);
        Assert.Null(updated.HelperMentorCommentId);
        Assert.Null(updated.StudentCommentId);

        mainUser.SetupGet(c => c.UserId).Returns(helperMentor);

        var helperId = await mainSvc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("helper") });
        await ctx.SaveChangesAsync();

        Assert.NotEqual(mainId, helperId);
        Assert.Equal(helperId, updated.HelperMentorCommentId);
    }

    [Fact]
    public async Task SaveComment_Throws_NotFound_When_Caller_Is_Not_On_The_Work()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        currentUser.SetupGet(c => c.UserId).Returns(Ulid.NewUlid());
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), Ulid.NewUlid());

        await Assert.ThrowsAsync<Noo.Api.Core.Exceptions.Http.NotFoundException>(
            () => svc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("nope") })
        );
    }

    [Fact]
    public async Task SaveComment_AsMentor_Publishes_StartedChecking_Only_On_First_Save()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);

        await svc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("checking") });
        await ctx.SaveChangesAsync();

        var started = Assert.Single(publisher.Published.OfType<StartedCheckingEvent>());
        Assert.Equal(mentor.Id, started.MentorId);
        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(AssignedWorkCheckStatus.InProgress, updated!.CheckStatus);

        await svc.Editing.SaveCommentAsync(aw.Id, new UpsertAssignedWorkCommentDTO { Content = RichTextFactory.Create("still checking") });
        await ctx.SaveChangesAsync();
        Assert.Single(publisher.Published.OfType<StartedCheckingEvent>());
    }

    [Fact]
    public async Task Get_Hides_Mentor_Comments_From_The_Student_Until_The_Work_Is_Checked()
    {
        var (svc, ctx, _, currentUser, _) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid(), solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        var mentorComment = new AssignedWorkCommentModel { Content = RichTextFactory.Create("draft") };
        ctx.GetDbSet<AssignedWorkCommentModel>().Add(mentorComment);
        aw.MainMentorCommentId = mentorComment.Id;
        ctx.SaveChanges();

        var hidden = await svc.Work.GetAsync(aw.Id);
        Assert.Null(hidden!.MainMentorComment);
        Assert.Null(hidden.MainMentorCommentId);

        aw.CheckStatus = AssignedWorkCheckStatus.CheckedInDeadline;
        ctx.SaveChanges();

        var shown = await svc.Work.GetAsync(aw.Id);
        Assert.Equal(mentorComment.Id, shown!.MainMentorCommentId);
        Assert.Equal("draft", shown.MainMentorComment!.Content!.ToString());
    }

    [Fact]
    public async Task MarkAsChecked_Publishes_CheckedEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, studentId: Ulid.NewUlid(), mainMentorId: mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.MarkAsCheckedAsync(aw.Id);

        var checkedEvent = Assert.Single(publisher.Published.OfType<CheckedEvent>());
        Assert.Equal(aw.Id, checkedEvent.AssignedWorkId);
        Assert.Equal(mentor.Id, checkedEvent.MentorId);
    }

    [Fact]
    public async Task ReturnToCheck_Publishes_SentOnRecheckEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline, checkStatus: AssignedWorkCheckStatus.CheckedInDeadline);
        aw.SolvedAt = Clock.Now; aw.CheckedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.ReturnToCheckAsync(aw.Id);

        var evt = Assert.Single(publisher.Published.OfType<SentOnRecheckEvent>());
        Assert.Equal(aw.Id, evt.AssignedWorkId);
        Assert.Equal(mentor.Id, evt.MentorId);
    }

    [Fact]
    public async Task ReturnToSolve_Publishes_SentOnResolveEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id, solveStatus: AssignedWorkSolveStatus.SolvedInDeadline);
        aw.SolvedAt = Clock.Now; ctx.SaveChanges();

        await svc.Lifecycle.ReturnToSolveAsync(aw.Id);

        var evt = Assert.Single(publisher.Published.OfType<SentOnResolveEvent>());
        Assert.Equal(aw.Id, evt.AssignedWorkId);
        Assert.Equal(mentor.Id, evt.MentorId);
    }

    [Fact]
    public async Task AddHelperMentor_Publishes_HelperMentorAddedEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        var newHelper = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(newHelper);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await svc.Mentors.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = newHelper.Id });

        var evt = Assert.Single(publisher.Published.OfType<HelperMentorAddedEvent>());
        Assert.Equal(aw.Id, evt.AssignedWorkId);
        Assert.Equal(newHelper.Id, evt.MentorId);
        Assert.Equal(mainMentor.Id, evt.ChangedById);
    }

    [Fact]
    public async Task AddHelperMentor_NoOp_Does_Not_Publish_Event()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var mainMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mainMentor);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mainMentor.Id);
        var aw = SeedAssignedWork(ctx, student.Id, mainMentor.Id);

        await svc.Mentors.AddHelperMentorAsync(aw.Id, new AddHelperMentorOptionsDTO { MentorId = mainMentor.Id });

        Assert.Empty(publisher.Published.OfType<HelperMentorAddedEvent>());
    }

    [Fact]
    public async Task ReplaceMainMentor_Publishes_MainMentorChangedEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Teacher);
        var teacher = MakeUser(UserRoles.Teacher); ctx.GetDbSet<UserModel>().Add(teacher);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student);
        var oldMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(oldMentor);
        var newMentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(newMentor);
        ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(teacher.Id);
        var aw = SeedAssignedWork(ctx, student.Id, oldMentor.Id);

        await svc.Mentors.ReplaceMainMentorAsync(aw.Id, new ReplaceMainMentorOptionsDTO { MentorId = newMentor.Id });

        var updated = await ctx.GetDbSet<AssignedWorkModel>().FindAsync(aw.Id);
        Assert.Equal(newMentor.Id, updated!.MainMentorId);
        var evt = Assert.Single(publisher.Published.OfType<MainMentorChangedEvent>());
        Assert.Equal(aw.Id, evt.AssignedWorkId);
        Assert.Equal(newMentor.Id, evt.NewMentorId);
        Assert.Equal(oldMentor.Id, evt.OldMentorId);
        Assert.Equal(teacher.Id, evt.ChangedById);
    }

    [Fact]
    public async Task ShiftDeadline_AsStudent_Publishes_DeadlineShiftedEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Student);
        var student = MakeUser(UserRoles.Student); ctx.GetDbSet<UserModel>().Add(student); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(student.Id);
        var aw = SeedAssignedWork(ctx, student.Id, Ulid.NewUlid());
        var newDeadline = aw.SolveDeadlineAt!.Value.Add(AssignedWorkConfig.MaxSolveDeadlineShift).AddMinutes(-1);

        await svc.Lifecycle.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline, NotifyOthers = true });

        var evt = Assert.Single(publisher.Published.OfType<DeadlineShiftedEvent>());
        Assert.Equal(aw.Id, evt.AssignedWorkId);
        Assert.Equal(UserRoles.Student, evt.Payload.ShiftedByRole);
        Assert.Equal(student.Id, evt.Payload.ShiftedById);
        Assert.Equal(newDeadline, evt.Payload.NewDeadlineAt);
    }

    [Fact]
    public async Task ShiftDeadline_AsMentor_Publishes_DeadlineShiftedEvent()
    {
        var (svc, ctx, _, currentUser, publisher) = CreateService(UserRoles.Mentor);
        var mentor = MakeUser(UserRoles.Mentor); ctx.GetDbSet<UserModel>().Add(mentor); ctx.SaveChanges();
        currentUser.SetupGet(c => c.UserId).Returns(mentor.Id);
        var aw = SeedAssignedWork(ctx, Ulid.NewUlid(), mentor.Id);
        var newDeadline = aw.CheckDeadlineAt!.Value.Add(AssignedWorkConfig.MaxCheckDeadlineShift).AddMinutes(-1);

        await svc.Lifecycle.ShiftDeadlineAsync(aw.Id, new ShiftAssignedWorkDeadlineOptionsDTO { NewDeadline = newDeadline });

        var evt = Assert.Single(publisher.Published.OfType<DeadlineShiftedEvent>());
        Assert.Equal(UserRoles.Mentor, evt.Payload.ShiftedByRole);
        Assert.Equal(mentor.Id, evt.Payload.ShiftedById);
        Assert.Equal(newDeadline, evt.Payload.NewDeadlineAt);
    }
}
