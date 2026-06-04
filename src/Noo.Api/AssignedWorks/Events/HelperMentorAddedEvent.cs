using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;

namespace Noo.Api.AssignedWorks.Events;

public sealed record HelperMentorAddedEvent(Ulid AssignedWorkId, Ulid MentorId, Ulid ChangedById)
    : IDomainEvent;

public sealed class HelperMentorAddedHistoryHandler : IEventHandler<HelperMentorAddedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public HelperMentorAddedHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(HelperMentorAddedEvent @event, CancellationToken ct = default)
    {
        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.ChangedById,
                Type = AssignedWorkHistoryType.HelperMentorAdded,
                ChangedAt = Clock.Now,
                Value = new Dictionary<string, string>
                {
                    ["mentorId"] = @event.MentorId.ToString(),
                },
            }
        );

        return Task.CompletedTask;
    }
}

public sealed class HelperMentorAddedNotificationHandler : IEventHandler<HelperMentorAddedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkLinkGenerator _linkGenerator;

    public HelperMentorAddedNotificationHandler(
        INotificationService notificationService,
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkLinkGenerator linkGenerator
    )
    {
        _notificationService = notificationService;
        _assignedWorkRepository = assignedWorkRepository;
        _linkGenerator = linkGenerator;
    }

    public async Task HandleAsync(HelperMentorAddedEvent @event, CancellationToken ct = default)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(@event.AssignedWorkId);

        if (assignedWork is null)
        {
            return;
        }

        await _notificationService.CreateNotificationAsync(
            new CreateNotificationDTO
            {
                UserId = @event.MentorId,
                Type = "assigned_work.helper_mentor_added",
                Title = "Вы назначены помощником",
                Message =
                    $"Вы назначены помощником-куратором на работу \"{assignedWork.Title}\".",
                Link = _linkGenerator.GenerateViewLink(assignedWork.Id),
                LinkText = "Перейти к работе",
            }
        );
    }
}
