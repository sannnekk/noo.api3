using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;

namespace Noo.Api.AssignedWorks.Events;

public sealed record AssignedWorkSolvedEvent(Ulid AssignedWorkId, Ulid StudentId) : IDomainEvent;

/// <summary>
/// Adds an entry into the assigned work history when a work is solved.
/// </summary>
public sealed class AssignedWorkSolvedHistoryHandler : IEventHandler<AssignedWorkSolvedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public AssignedWorkSolvedHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(AssignedWorkSolvedEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkStatusHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.StudentId,
                Type = AssignedWorkStatusHistoryType.Solved,
                ChangedAt = Clock.Now,
            }
        );

        return Task.CompletedTask;
    }
}

/// <summary>
/// Notifies the student and mentors when a work is solved.
/// </summary>
public sealed class AssignedWorkSolvedNotificationHandler : IEventHandler<AssignedWorkSolvedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkLinkGenerator _linkGenerator;

    public AssignedWorkSolvedNotificationHandler(
        INotificationService notificationService,
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkLinkGenerator linkGenerator
    )
    {
        _notificationService = notificationService;
        _assignedWorkRepository = assignedWorkRepository;
        _linkGenerator = linkGenerator;
    }

    public async Task HandleAsync(AssignedWorkSolvedEvent @event, CancellationToken ct = default)
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
                Type = "assigned_work.solved",
                Title = "Работа сдана",
                Message = $"Работа \"{assignedWork.Title}\" была сдана и ожидает проверки.",
                Link = _linkGenerator.GenerateViewLink(assignedWork.Id),
                LinkText = "Перейти к работе",
            }
        );
    }
}
