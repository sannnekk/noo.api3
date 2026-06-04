using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Services;

namespace Noo.Api.AssignedWorks.Events;

public sealed record MainMentorChangedEvent(
    Ulid AssignedWorkId,
    Ulid NewMentorId,
    Ulid? OldMentorId,
    Ulid ChangedById
) : IDomainEvent;

public sealed class MainMentorChangedHistoryHandler : IEventHandler<MainMentorChangedEvent>
{
    private readonly IAssignedWorkHistoryRepository _historyRepository;

    public MainMentorChangedHistoryHandler(IAssignedWorkHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public Task HandleAsync(MainMentorChangedEvent @event, CancellationToken ct = default)
    {
        var value = new Dictionary<string, string>
        {
            ["newMentorId"] = @event.NewMentorId.ToString(),
        };

        if (@event.OldMentorId.HasValue)
        {
            value["oldMentorId"] = @event.OldMentorId.Value.ToString();
        }

        _historyRepository.Add(
            new AssignedWorkHistoryModel
            {
                AssignedWorkId = @event.AssignedWorkId,
                ChangedById = @event.ChangedById,
                Type = AssignedWorkHistoryType.MainMentorChanged,
                ChangedAt = Clock.Now,
                Value = value,
            }
        );

        return Task.CompletedTask;
    }
}

public sealed class MainMentorChangedNotificationHandler : IEventHandler<MainMentorChangedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkLinkGenerator _linkGenerator;

    public MainMentorChangedNotificationHandler(
        INotificationService notificationService,
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkLinkGenerator linkGenerator
    )
    {
        _notificationService = notificationService;
        _assignedWorkRepository = assignedWorkRepository;
        _linkGenerator = linkGenerator;
    }

    public async Task HandleAsync(MainMentorChangedEvent @event, CancellationToken ct = default)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(@event.AssignedWorkId);

        if (assignedWork is null)
        {
            return;
        }

        var link = _linkGenerator.GenerateViewLink(assignedWork.Id);

        await _notificationService.CreateNotificationAsync(
            new CreateNotificationDTO
            {
                UserId = @event.NewMentorId,
                Type = "assigned_work.main_mentor_changed",
                Title = "Вы назначены куратором",
                Message =
                    $"Вы назначены основным куратором на работу \"{assignedWork.Title}\".",
                Link = link,
                LinkText = "Перейти к работе",
            }
        );

        if (@event.OldMentorId.HasValue && @event.OldMentorId.Value != @event.NewMentorId)
        {
            await _notificationService.CreateNotificationAsync(
                new CreateNotificationDTO
                {
                    UserId = @event.OldMentorId.Value,
                    Type = "assigned_work.main_mentor_changed",
                    Title = "Вы больше не куратор",
                    Message =
                        $"Вы больше не являетесь основным куратором на работе \"{assignedWork.Title}\".",
                    Link = link,
                    LinkText = "Перейти к работе",
                }
            );
        }
    }
}
