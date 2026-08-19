using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.UnitTests.Common;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkRepositoryTests
{
    [Fact]
    public async Task GetByWorkAssignmentAsync_Returns_For_Student()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var repo = new AssignedWorkRepository(ctx);
        var studentId = Ulid.NewUlid();
        var workAssignmentId = Ulid.NewUlid();
        var aw = new AssignedWorkModel
        {
            Title = "X",
            Type = Noo.Api.Works.Types.WorkType.Test,
            Attempt = 2,
            StudentId = studentId,
            MainMentorId = Ulid.NewUlid(),
            CourseWorkAssignmentId = workAssignmentId,
            SolveStatus = AssignedWorkSolveStatus.InProgress,
            CheckStatus = AssignedWorkCheckStatus.NotChecked,
            MaxScore = 50
        };
        ctx.GetDbSet<AssignedWorkModel>().Add(aw);
        await ctx.SaveChangesAsync();
        var works = await repo.GetByWorkAssignmentAsync(workAssignmentId, studentId);
        Assert.NotNull(works);
        Assert.Equal(aw.Attempt, works.Single().Attempt);
    }
}
