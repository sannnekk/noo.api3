using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkRepositoryTests
{
    [Fact]
    public async Task GetProgressAsync_Returns_For_Student()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var repo = uow.AssignedWorkRepository();
        var studentId = Ulid.NewUlid();
        var aw = new AssignedWorkModel
        {
            Title = "X",
            Type = Noo.Api.Works.Types.WorkType.Test,
            Attempt = 2,
            StudentId = studentId,
            MainMentorId = Ulid.NewUlid(),
            SolveStatus = AssignedWorkSolveStatus.InProgress,
            CheckStatus = AssignedWorkCheckStatus.NotChecked,
            MaxScore = 50
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        await ctx.SaveChangesAsync();
        var progress = await repo.GetProgressAsync(aw.Id, studentId);
        Assert.NotNull(progress);
        Assert.Equal(aw.Attempt, progress!.Attempt);
    }

    [Fact]
    public async Task GetAsync_Hides_NotSubmitted_Answer_Fields()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var repo = uow.AssignedWorkRepository();
        var aw = new AssignedWorkModel
        {
            Title = "Test",
            Type = Noo.Api.Works.Types.WorkType.Test,
            Attempt = 1,
            StudentId = Ulid.NewUlid(),
            MainMentorId = Ulid.NewUlid(),
            MaxScore = 10
        };
        var answer = new AssignedWorkAnswerModel
        {
            AssignedWorkId = aw.Id,
            TaskId = Ulid.NewUlid(),
            Status = AssignedWorkAnswerStatus.NotSubmitted,
            Score = 5,
            MaxScore = 10,
            DetailedScore = new Dictionary<string, int> { { "a", 1 } }
        };
        // Link the answer via navigation to ensure relationship is established for Include()
        aw.Answers.Add(answer);
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        await ctx.SaveChangesAsync();
        var fetched = await repo.GetAsync(aw.Id);
        Assert.NotNull(fetched);
        var ans = Assert.Single(fetched!.Answers);
        Assert.Null(ans.Score);
        Assert.Null(ans.DetailedScore);
    }
}
