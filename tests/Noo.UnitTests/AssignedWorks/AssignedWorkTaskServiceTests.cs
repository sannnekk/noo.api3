using Moq;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkTaskServiceTests
{
    private static (AssignedWorkTaskService svc, NooDbContext ctx, Ulid studentId) CreateService(
        Ulid? asUser = null
    )
    {
        var ctx = TestHelpers.CreateInMemoryDb();
        var studentId = Ulid.NewUlid();
        var currentUser = new Mock<ICurrentUser> { CallBase = true };
        currentUser.SetupGet(c => c.UserId).Returns(asUser ?? studentId);
        currentUser.SetupGet(c => c.UserRole).Returns(UserRoles.Student);
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);

        var svc = new AssignedWorkTaskService(
            new AssignedWorkRepository(ctx),
            new TaskCheckService(),
            currentUser.Object
        );

        return (svc, ctx, studentId);
    }

    private static (AssignedWorkModel work, WorkTaskModel task, AssignedWorkAnswerModel? answer) Seed(
        NooDbContext ctx,
        Ulid studentId,
        bool showAnswerBeforeCheck = false,
        bool checkOneByOne = false,
        string? givenAnswer = "answer",
        AssignedWorkAnswerStatus answerStatus = AssignedWorkAnswerStatus.NotSubmitted,
        AssignedWorkSolveStatus solveStatus = AssignedWorkSolveStatus.InProgress,
        WorkTaskType taskType = WorkTaskType.Word
    )
    {
        var work = new WorkModel { Title = "W", Type = WorkType.Test };
        ctx.GetDbSet<WorkModel>().Add(work);

        var task = new WorkTaskModel
        {
            Content = RichTextFactory.Create("q"),
            Type = taskType,
            CheckStrategy = WorkTaskCheckStrategy.ExactMatchOrZero,
            RightAnswers = ["answer", "ANSWER TOO"],
            Order = 0,
            MaxScore = 10,
            WorkId = work.Id,
            ShowAnswerBeforeCheck = showAnswerBeforeCheck,
            CheckOneByOne = checkOneByOne,
        };
        ctx.GetDbSet<WorkTaskModel>().Add(task);

        var assignedWork = new AssignedWorkModel
        {
            Title = "AW",
            Type = WorkType.Test,
            Attempt = 1,
            StudentId = studentId,
            MainMentorId = Ulid.NewUlid(),
            WorkId = work.Id,
            SolveStatus = solveStatus,
            SolvedAt = AssignedWorkStatuses.Solved.Contains(solveStatus)
                ? Noo.Api.Core.Utils.Clock.Now
                : null,
            MaxScore = 10,
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(assignedWork);

        AssignedWorkAnswerModel? answer = null;

        if (givenAnswer != null)
        {
            answer = new AssignedWorkAnswerModel
            {
                AssignedWorkId = assignedWork.Id,
                TaskId = task.Id,
                WordContent = givenAnswer,
                MaxScore = 10,
                Status = answerStatus,
            };
            ctx.GetDbSet<AssignedWorkAnswerModel>().Add(answer);
        }

        ctx.SaveChanges();

        return (assignedWork, task, answer);
    }

    [Fact]
    public async Task AnswerKey_Is_Given_For_A_Task_That_Offers_It()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, _) = Seed(ctx, studentId, showAnswerBeforeCheck: true);

        var key = await svc.GetAnswerKeyAsync(work.Id, task.Id);

        Assert.Equal(task.Id, key.TaskId);
        Assert.Equal(["answer", "ANSWER TOO"], key.RightAnswers);
    }

    [Fact]
    public async Task AnswerKey_Is_Refused_For_A_Task_That_Does_Not_Offer_It()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, _) = Seed(ctx, studentId, showAnswerBeforeCheck: false);

        await Assert.ThrowsAsync<TaskAnswerKeyNotOfferedException>(
            () => svc.GetAnswerKeyAsync(work.Id, task.Id)
        );
    }

    [Fact]
    public async Task AnswerKey_Of_Someone_Elses_Work_Is_Not_Found()
    {
        var (svc, ctx, _) = CreateService(asUser: Ulid.NewUlid());
        var (work, task, _) = Seed(ctx, Ulid.NewUlid(), showAnswerBeforeCheck: true);

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.GetAnswerKeyAsync(work.Id, task.Id)
        );
    }

    [Fact]
    public async Task Check_Scores_The_Answer_And_Locks_It()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, answer) = Seed(ctx, studentId, checkOneByOne: true);

        var verdict = await svc.CheckAsync(work.Id, task.Id);
        await ctx.SaveChangesAsync();

        Assert.Equal(10, verdict.Score);
        Assert.Equal(10, verdict.MaxScore);
        Assert.True(verdict.IsCorrect);
        Assert.Equal(answer!.Id, verdict.AnswerId);

        var stored = await ctx.GetDbSet<AssignedWorkAnswerModel>().FindAsync(answer.Id);
        Assert.Equal(AssignedWorkAnswerStatus.Checked, stored!.Status);
        Assert.Equal(10, stored.Score);
    }

    [Fact]
    public async Task Check_Of_A_Wrong_Answer_Scores_Zero_And_Still_Locks_It()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, answer) = Seed(ctx, studentId, checkOneByOne: true, givenAnswer: "nope");

        var verdict = await svc.CheckAsync(work.Id, task.Id);
        await ctx.SaveChangesAsync();

        Assert.Equal(0, verdict.Score);
        Assert.False(verdict.IsCorrect);
        var stored = await ctx.GetDbSet<AssignedWorkAnswerModel>().FindAsync(answer!.Id);
        Assert.Equal(AssignedWorkAnswerStatus.Checked, stored!.Status);
    }

    [Fact]
    public async Task Check_Twice_Stands_By_The_Verdict_Already_Given()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, answer) = Seed(ctx, studentId, checkOneByOne: true);

        await svc.CheckAsync(work.Id, task.Id);
        await ctx.SaveChangesAsync();

        // The student can no longer edit the answer, so a second click must not rescore it.
        answer!.WordContent = "tampered";
        await ctx.SaveChangesAsync();

        var second = await svc.CheckAsync(work.Id, task.Id);

        Assert.Equal(10, second.Score);
    }

    [Fact]
    public async Task Check_Is_Refused_For_A_Task_Not_Marked_One_By_One()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, _) = Seed(ctx, studentId, checkOneByOne: false);

        await Assert.ThrowsAsync<TaskNotCheckableOnItsOwnException>(
            () => svc.CheckAsync(work.Id, task.Id)
        );
    }

    [Fact]
    public async Task Check_Is_Refused_Once_The_Work_Has_Been_Handed_In()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, _) = Seed(
            ctx,
            studentId,
            checkOneByOne: true,
            solveStatus: AssignedWorkSolveStatus.SolvedInDeadline
        );

        await Assert.ThrowsAsync<AssignedWorkAlreadySolvedException>(
            () => svc.CheckAsync(work.Id, task.Id)
        );
    }

    [Fact]
    public async Task Check_Is_Refused_When_There_Is_No_Answer_Yet()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, _) = Seed(ctx, studentId, checkOneByOne: true, givenAnswer: null);

        await Assert.ThrowsAsync<TaskNotAnsweredException>(
            () => svc.CheckAsync(work.Id, task.Id)
        );
    }

    [Fact]
    public async Task Check_Is_Refused_For_A_Task_No_Checker_Can_Score()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, task, _) = Seed(
            ctx,
            studentId,
            checkOneByOne: true,
            taskType: WorkTaskType.Essay
        );

        await Assert.ThrowsAsync<TaskNotCheckableOnItsOwnException>(
            () => svc.CheckAsync(work.Id, task.Id)
        );
    }

    [Fact]
    public async Task Check_Of_A_Task_From_Another_Work_Is_Not_Found()
    {
        var (svc, ctx, studentId) = CreateService();
        var (work, _, _) = Seed(ctx, studentId, checkOneByOne: true);

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.CheckAsync(work.Id, Ulid.NewUlid())
        );
    }
}
