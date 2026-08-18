using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Utils;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkStatusesTests
{
    [Fact]
    public void SolvedAt_Before_Deadline_Is_InDeadline()
    {
        var now = Clock.Now;

        Assert.Equal(
            AssignedWorkSolveStatus.SolvedInDeadline,
            AssignedWorkStatuses.SolvedAt(now, now.AddMinutes(1))
        );
    }

    [Fact]
    public void SolvedAt_After_Deadline_Is_AfterDeadline()
    {
        var now = Clock.Now;

        Assert.Equal(
            AssignedWorkSolveStatus.SolvedAfterDeadline,
            AssignedWorkStatuses.SolvedAt(now, now.AddMinutes(-1))
        );
    }

    [Fact]
    public void SolvedAt_Without_Deadline_Is_InDeadline()
    {
        Assert.Equal(
            AssignedWorkSolveStatus.SolvedInDeadline,
            AssignedWorkStatuses.SolvedAt(Clock.Now, null)
        );
    }

    [Fact]
    public void CheckedAt_After_Deadline_Is_AfterDeadline()
    {
        var now = Clock.Now;

        Assert.Equal(
            AssignedWorkCheckStatus.CheckedAfterDeadline,
            AssignedWorkStatuses.CheckedAt(now, now.AddMinutes(-1))
        );
    }

    [Fact]
    public void CheckedAt_Without_Deadline_Is_InDeadline()
    {
        Assert.Equal(
            AssignedWorkCheckStatus.CheckedInDeadline,
            AssignedWorkStatuses.CheckedAt(Clock.Now, null)
        );
    }

    [Fact]
    public void Status_Groups_Cover_Every_Member_Exactly_Once()
    {
        Assert.Equal(
            Enum.GetValues<AssignedWorkSolveStatus>().Order(),
            AssignedWorkStatuses.Unsolved.Concat(AssignedWorkStatuses.Solved).Order()
        );

        Assert.Equal(
            Enum.GetValues<AssignedWorkCheckStatus>().Order(),
            AssignedWorkStatuses.Unchecked.Concat(AssignedWorkStatuses.Checked).Order()
        );
    }
}
