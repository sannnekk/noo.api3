using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;

namespace Noo.Api.AssignedWorks.Events;

public sealed record DeadlineShiftedEvent(Ulid AssignedWorkId, ShiftDeadlinePayload Payload)
    : IDomainEvent;

public sealed class DeadlineShiftedHistoryHandler : IEventHandler<DeadlineShiftedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public DeadlineShiftedHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(DeadlineShiftedEvent @event, CancellationToken ct = default)
    {
        var Type = @event.Payload.ShiftedByRole switch
        {
            UserRoles.Student => AssignedWorkHistoryType.SolveDeadlineShifted,
            UserRoles.Mentor => AssignedWorkHistoryType.CheckDeadlineShifted,
            _ => throw new ArgumentException(
                "Invalid ShiftDeadlineType",
                nameof(@event.Payload.ShiftedByRole)
            ),
        };

        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.Payload.ShiftedById,
                Type = Type,
                ChangedAt = Clock.Now,
                Value = new Dictionary<string, string>
                {
                    ["newDeadline"] = @event.Payload.NewDeadlineAt.ToString("o"),
                },
            }
        );

        return Task.CompletedTask;
    }
}
