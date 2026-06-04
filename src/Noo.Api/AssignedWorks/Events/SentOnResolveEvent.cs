using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;

namespace Noo.Api.AssignedWorks.Events;

public sealed record SentOnResolveEvent(Ulid AssignedWorkId, Ulid MentorId) : IDomainEvent;

public sealed class SentOnResolveHistoryHandler : IEventHandler<SentOnResolveEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public SentOnResolveHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(SentOnResolveEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.MentorId,
                Type = AssignedWorkHistoryType.SentOnResolve,
                ChangedAt = Clock.Now,
            }
        );

        return Task.CompletedTask;
    }
}

public sealed class SentOnResolveNotificationHandler : IEventHandler<SentOnResolveEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkLinkGenerator _linkGenerator;

    public SentOnResolveNotificationHandler(
        INotificationService notificationService,
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkLinkGenerator linkGenerator
    )
    {
        _notificationService = notificationService;
        _assignedWorkRepository = assignedWorkRepository;
        _linkGenerator = linkGenerator;
    }

    public async Task HandleAsync(SentOnResolveEvent @event, CancellationToken ct = default)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(@event.AssignedWorkId);

        if (assignedWork is null)
        {
            return;
        }

        await _notificationService.CreateNotificationAsync(
            new CreateNotificationDTO
            {
                UserId = assignedWork.StudentId,
                Type = "assigned_work.sent_on_resolve",
                Title = "Работа возвращена на доработку",
                Message = $"Работа \"{assignedWork.Title}\" возвращена вам на доработку.",
                Link = _linkGenerator.GenerateViewLink(assignedWork.Id),
                LinkText = "Перейти к работе",
            }
        );
    }
}
