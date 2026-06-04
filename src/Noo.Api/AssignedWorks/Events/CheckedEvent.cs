using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;

namespace Noo.Api.AssignedWorks.Events;

public sealed record CheckedEvent(Ulid AssignedWorkId, Ulid MentorId) : IDomainEvent;

public sealed class CheckedHistoryHandler : IEventHandler<CheckedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public CheckedHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(CheckedEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.MentorId,
                Type = AssignedWorkHistoryType.Checked,
                ChangedAt = Clock.Now,
            }
        );

        return Task.CompletedTask;
    }
}

public sealed class CheckedNotificationHandler : IEventHandler<CheckedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkLinkGenerator _linkGenerator;

    public CheckedNotificationHandler(
        INotificationService notificationService,
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkLinkGenerator linkGenerator
    )
    {
        _notificationService = notificationService;
        _assignedWorkRepository = assignedWorkRepository;
        _linkGenerator = linkGenerator;
    }

    public async Task HandleAsync(CheckedEvent @event, CancellationToken ct = default)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(@event.AssignedWorkId);

        if (assignedWork is null)
        {
            return;
        }

        var recipientIds = new[]
        {
            (Ulid?)assignedWork.StudentId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId,
        }
            .Where(id => id.HasValue)
            .Select(id => id!.Value);

        await _notificationService.BulkCreateNotificationsAsync(
            new BulkCreateNotificationsDTO
            {
                UserIds = recipientIds,
                Type = "assigned_work.checked",
                Title = "Работа проверена",
                Message =
                    $"Работа \"{assignedWork.Title}\" проверена, балл: {assignedWork.Score}/{assignedWork.MaxScore} ({assignedWork.PercentegeScore}).",
                Link = _linkGenerator.GenerateViewLink(assignedWork.Id),
                LinkText = "Перейти к работе",
            }
        );
    }
}
