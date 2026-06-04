using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;

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
