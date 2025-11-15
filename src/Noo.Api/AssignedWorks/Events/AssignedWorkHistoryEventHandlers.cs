using MediatR;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;

namespace Noo.Api.AssignedWorks.Events;

public class AssignedWorkHistoryEventHandlers :
    INotificationHandler<HelperMentorRemovedEvent>,
    INotificationHandler<HelperMentorAddedEvent>,
    INotificationHandler<AssignedWorkCheckedEvent>,
    INotificationHandler<AssignedWorkSolvedEvent>,
    INotificationHandler<MainMentorChangedEvent>,
    INotificationHandler<AssignedWorkCheckDeadlineShiftedEvent>,
    INotificationHandler<AssignedWorkSolveDeadlineShiftedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public AssignedWorkHistoryEventHandlers(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task Handle(HelperMentorRemovedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.HelperMentorRemoved,
            ChangedAt = DateTime.UtcNow
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }

    public Task Handle(AssignedWorkCheckedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.Checked,
            ChangedAt = DateTime.UtcNow,
            ChangedById = @event.CheckedBy
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }

    public Task Handle(AssignedWorkCheckDeadlineShiftedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.CheckDeadlineShifted,
            ChangedAt = DateTime.UtcNow,
            ChangedById = @event.ShiftedById
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }

    public Task Handle(AssignedWorkSolveDeadlineShiftedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.SolveDeadlineShifted,
            ChangedAt = DateTime.UtcNow
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }

    public Task Handle(MainMentorChangedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.MainMentorChanged,
            ChangedAt = DateTime.UtcNow,
            Value = new Dictionary<string, string>
            {
                { "oldMentorId", @event.OldMentorId.ToString() },
                { "newMentorId", @event.NewMentorId.ToString() }
            }
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }

    public Task Handle(AssignedWorkSolvedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.Solved,
            ChangedAt = DateTime.UtcNow
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }

    public Task Handle(HelperMentorAddedEvent @event, CancellationToken cancellationToken)
    {
        var historyEntry = new AssignedWorkStatusHistoryModel()
        {
            AssignedWorkId = @event.AssignedWorkId,
            Type = AssignedWorkStatusHistoryType.HelperMentorAdded,
            ChangedAt = DateTime.UtcNow,
            Value = new Dictionary<string, string>
            {
                { "newMentorId", @event.HelperMentorId.ToString() }
            }
        };

        _historyRepository.Add(historyEntry);
        return Task.CompletedTask;
    }
}
