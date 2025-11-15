using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Types;
using Noo.UnitTests.Common;
using Noo.Api.AssignedWorks.Services;

namespace Noo.UnitTests.AssignedWorks;

public class AssignedWorkHistoryEventHandlersTests
{
    [Fact]
    public async Task Publish_Events_Adds_History_Entries()
    {
        using var ctx = TestHelpers.CreateInMemoryDb();
        var uow = TestHelpers.CreateUowMock(ctx).Object;
        var historyRepo = new AssignedWorkHistoryRepository(ctx);
        var handler = new AssignedWorkHistoryEventHandlers(historyRepo);
        var awId = Ulid.NewUlid();
        // simulate various events
        await handler.Handle(new AssignedWorkSolvedEvent(awId), CancellationToken.None);
        await handler.Handle(new AssignedWorkCheckedEvent(awId, Ulid.NewUlid()), CancellationToken.None);
        await handler.Handle(new AssignedWorkSolveDeadlineShiftedEvent(awId, false), CancellationToken.None);
        await handler.Handle(new AssignedWorkCheckDeadlineShiftedEvent(awId, Ulid.NewUlid(), false), CancellationToken.None);
        await handler.Handle(new MainMentorChangedEvent(Ulid.NewUlid(), Ulid.NewUlid(), awId, false, false), CancellationToken.None);
        await handler.Handle(new HelperMentorAddedEvent(Ulid.NewUlid(), awId), CancellationToken.None);
        await handler.Handle(new HelperMentorRemovedEvent(Ulid.NewUlid(), awId), CancellationToken.None);
        // Persist changes because handlers only Add to repository; in the app, UnitOfWork commit happens in service pipeline.
        await ctx.SaveChangesAsync();
        var repo = historyRepo;
        var history = (await repo.GetHistoryAsync(awId)).ToList();
        // 7 events
        Assert.Equal(7, history.Count);
        Assert.Contains(history, h => h.Type == AssignedWorkStatusHistoryType.Solved);
        Assert.Contains(history, h => h.Type == AssignedWorkStatusHistoryType.Checked);
    }
}
