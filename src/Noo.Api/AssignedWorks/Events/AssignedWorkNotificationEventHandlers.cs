using MediatR;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.Services;
using Noo.Api.Users.Services;

namespace Noo.Api.AssignedWorks.Events;

public class AssignedWorkNotificationEventHandlers :
    INotificationHandler<HelperMentorRemovedEvent>,
    INotificationHandler<HelperMentorAddedEvent>,
    INotificationHandler<AssignedWorkCheckedEvent>,
    INotificationHandler<AssignedWorkSolvedEvent>,
    INotificationHandler<MainMentorChangedEvent>,
    INotificationHandler<AssignedWorkCheckDeadlineShiftedEvent>,
    INotificationHandler<AssignedWorkSolveDeadlineShiftedEvent>
{
    private readonly INotificationService _notificationService;

    private readonly IAssignedWorkRepository _assignedWorkRepository;

    private readonly IUserRepository _userRepository;

    private readonly IAssignedWorkLinkGenerator _assignedWorkLinkGenerator;

    public AssignedWorkNotificationEventHandlers(INotificationService notificationService, IUnitOfWork unitOfWork, IUserRepository userRepository, IAssignedWorkLinkGenerator assignedWorkLinkGenerator)
    {
        _notificationService = notificationService;
        _assignedWorkLinkGenerator = assignedWorkLinkGenerator;
        _assignedWorkRepository = unitOfWork.AssignedWorkRepository();
        _userRepository = userRepository;
    }

    public async Task Handle(HelperMentorRemovedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(notification.AssignedWorkId);

        if (assignedWork == null)
        {
            return;
        }

        await _notificationService.CreateNotificationAsync(new()
        {
            UserId = notification.RemovedMentorId,
            Type = "assigned_work.helper_mentor_removed",
            Title = "Вы удалены как помощник куратора",
            Message = $"Вы больше не помощник куратора для работы \"{assignedWork.Title}\"."
        });
    }

    public async Task Handle(AssignedWorkCheckedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(notification.AssignedWorkId);

        if (assignedWork == null)
        {
            return;
        }

        var ToSendIds = new List<Ulid>()
        {
            assignedWork.StudentId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId.HasValue
                ? assignedWork.HelperMentorId.Value
                : Ulid.Empty
        }
            .Where(id => id != Ulid.Empty)
            .ToList();

        await _notificationService.BulkCreateNotificationsAsync(new()
        {
            UserIds = ToSendIds,
            Type = "assigned_work.checked",
            Title = "Работа проверена",
            Message = $"Работа \"{assignedWork.Title}\" была проверена, балл: {assignedWork.Score}/{assignedWork.MaxScore}.",
            Link = _assignedWorkLinkGenerator.GenerateViewLink(assignedWork.Id),
            LinkText = "Перейти к работе"
        });
    }

    public async Task Handle(AssignedWorkCheckDeadlineShiftedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(notification.AssignedWorkId);

        if (assignedWork == null || !assignedWork.CheckDeadlineAt.HasValue)
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(notification.ShiftedById);

        if (user == null)
        {
            return;
        }

        var newDeadline = DateTimeFormatter.FormatDate(assignedWork.CheckDeadlineAt.Value);
        var ToSendIds = new List<Ulid>()
        {
            assignedWork.StudentId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId ?? Ulid.Empty
        }
            .Where(id => id != Ulid.Empty)
            .ToList();

        await _notificationService.BulkCreateNotificationsAsync(new()
        {
            UserIds = ToSendIds,
            Type = "assigned_work.deadline_shifted",
            Title = "Дедлайн проверки работы сдвинут",
            Message = $"{user.Name} сдвинул(а) дедлайн проверки работы \"{assignedWork.Title}\" на {newDeadline}."
        });
    }

    public async Task Handle(AssignedWorkSolveDeadlineShiftedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetWithStudentAsync(notification.AssignedWorkId);

        if (assignedWork == null || !assignedWork.SolveDeadlineAt.HasValue)
        {
            return;
        }

        var newDeadline = DateTimeFormatter.FormatDate(assignedWork.SolveDeadlineAt.Value);
        var ToSendIds = new List<Ulid>()
        {
            assignedWork.StudentId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId ?? Ulid.Empty
        }
            .Where(id => id != Ulid.Empty)
            .ToList();

        await _notificationService.BulkCreateNotificationsAsync(new()
        {
            UserIds = ToSendIds,
            Type = "assigned_work.deadline_shifted",
            Title = "Дедлайн решения работы сдвинут",
            Message = $"{assignedWork.Student.Name} сдвинул(а) дедлайн решения работы \"{assignedWork.Title}\" на {newDeadline}."
        });
    }

    public async Task Handle(MainMentorChangedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(notification.AssignedWorkId);

        if (assignedWork == null || !assignedWork.SolveDeadlineAt.HasValue)
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(assignedWork.MainMentorId);

        if (user == null)
        {
            return;
        }

        var ToSendIds = new List<Ulid>()
        {
            assignedWork.StudentId,
            notification.OldMentorId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId ?? Ulid.Empty
        }
            .Where(id => id != Ulid.Empty)
            .ToList();

        await _notificationService.BulkCreateNotificationsAsync(new()
        {
            UserIds = ToSendIds,
            Type = "assigned_work.main_mentor_changed",
            Title = "Сменился главный куратор",
            Message = $"Теперь главным куратором работы \"{assignedWork.Title}\" является {user.Name}"
        });
    }

    public async Task Handle(AssignedWorkSolvedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(notification.AssignedWorkId);

        if (assignedWork == null)
        {
            return;
        }

        var ToSendIds = new List<Ulid>()
        {
            assignedWork.StudentId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId.HasValue
                ? assignedWork.HelperMentorId.Value
                : Ulid.Empty
        }
            .Where(id => id != Ulid.Empty)
            .ToList();

        await _notificationService.BulkCreateNotificationsAsync(new()
        {
            UserIds = ToSendIds,
            Type = "assigned_work.checked",
            Title = "Работа сдана",
            Message = $"Работа \"{assignedWork.Title}\" была сдана на проверку",
            LinkText = "Перейти к работе",
            Link = _assignedWorkLinkGenerator.GenerateViewLink(assignedWork.Id)
        });
    }

    public async Task Handle(HelperMentorAddedEvent notification, CancellationToken cancellationToken)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(notification.AssignedWorkId);

        if (assignedWork == null || !assignedWork.HelperMentorId.HasValue)
        {
            return;
        }

        var user = await _userRepository.GetByIdAsync(assignedWork.HelperMentorId.Value);

        if (user == null)
        {
            return;
        }

        var ToSendIds = new List<Ulid>()
        {
            assignedWork.StudentId,
            assignedWork.MainMentorId,
            assignedWork.HelperMentorId ?? Ulid.Empty
        }
            .Where(id => id != Ulid.Empty)
            .ToList();

        await _notificationService.BulkCreateNotificationsAsync(new()
        {
            UserIds = ToSendIds,
            Type = "assigned_work.helper_mentor_added",
            Title = "Добавлен помощник куратора",
            Message = $"{user.Name} теперь помощник куратора работы \"{assignedWork.Title}\""
        });
    }
}
