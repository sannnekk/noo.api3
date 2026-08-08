using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.UserHistory.Types;

namespace Noo.Api.AssignedWorks.Events;

public sealed record SentOnRecheckEvent(Ulid AssignedWorkId, Ulid MentorId) : IDomainEvent;

public sealed class SentOnRecheckHistoryHandler : IEventHandler<SentOnRecheckEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public SentOnRecheckHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(SentOnRecheckEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.MentorId,
                Type = AssignedWorkHistoryType.SentOnRecheck,
                ChangedAt = Clock.Now,
            }
        );

        return Task.CompletedTask;
    }
}

public sealed class SentOnRecheckUserHistoryHandler : IEventHandler<SentOnRecheckEvent>
{
    private readonly IAssignedWorkUserHistoryRecorder _recorder;

    public SentOnRecheckUserHistoryHandler(IAssignedWorkUserHistoryRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task HandleAsync(SentOnRecheckEvent @event, CancellationToken ct = default)
    {
        return _recorder.RecordAsync(
            @event.AssignedWorkId,
            @event.MentorId,
            UserHistoryType.WorkSentOnRecheck
        );
    }
}
