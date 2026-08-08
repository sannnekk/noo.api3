using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.AssignedWorks.Events;

public sealed record CreatedEvent(Ulid AssignedWorkId) : IDomainEvent;

public sealed class CreatedHistoryHandler : IEventHandler<CreatedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public CreatedHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(CreatedEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                Type = AssignedWorkHistoryType.Created,
                ChangedAt = Clock.Now,
            }
        );

        return Task.CompletedTask;
    }
}

public sealed class CreatedUserHistoryHandler : IEventHandler<CreatedEvent>
{
    private readonly IAssignedWorkUserHistoryRecorder _recorder;

    public CreatedUserHistoryHandler(IAssignedWorkUserHistoryRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task HandleAsync(CreatedEvent @event, CancellationToken ct = default)
    {
        // Assignment follows the course schedule rather than someone's action, so there is no actor.
        return _recorder.RecordAsync(@event.AssignedWorkId, null, UserHistoryType.WorkAssigned);
    }
}
