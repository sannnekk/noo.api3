using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;

namespace Noo.Api.AssignedWorks.Events;

public sealed record StartedSolvingEvent(Ulid AssignedWorkId, Ulid StudentId) : IDomainEvent;

public sealed class StartedSolvingHistoryHandler : IEventHandler<StartedSolvingEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public StartedSolvingHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(StartedSolvingEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.StudentId,
                Type = AssignedWorkHistoryType.StartedSolving,
                ChangedAt = Clock.Now,
            }
        );

        return Task.CompletedTask;
    }
}
