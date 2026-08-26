using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.Models;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkLifecycleService))]
public class AssignedWorkLifecycleService : IAssignedWorkLifecycleService
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly ITaskCheckService _taskCheckService;
    private readonly ICurrentUser _currentUser;
    private readonly IEventPublisher _events;

    public AssignedWorkLifecycleService(
        IAssignedWorkRepository assignedWorkRepository,
        ITaskCheckService taskCheckService,
        ICurrentUser currentUser,
        IEventPublisher events
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _taskCheckService = taskCheckService;
        _currentUser = currentUser;
        _events = events;
    }

    public async Task MarkAsSolvedAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetWithAnswersAndTasksAsync(
            assignedWorkId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (_currentUser.UserId != assignedWork.StudentId)
        {
            throw new NotFoundException();
        }

        if (assignedWork.IsSolved)
        {
            throw new AssignedWorkAlreadySolvedException();
        }

        foreach (var answer in assignedWork.Answers)
        {
            // Answers already checked one at a time stay checked; the rest are handed in.
            if (answer.Status != AssignedWorkAnswerStatus.Checked)
            {
                answer.Status = AssignedWorkAnswerStatus.Submitted;
            }
        }

        var check = _taskCheckService.CheckTasks(
            assignedWork.Answers,
            AssignedTasksOf(assignedWork)
        );

        var solvedAt = Clock.Now;

        assignedWork.Score = check.Score;
        assignedWork.SolvedAt = solvedAt;
        assignedWork.SolveStatus = AssignedWorkStatuses.SolvedAt(
            solvedAt,
            assignedWork.SolveDeadlineAt
        );

        await _events.PublishAsync(new SolvedEvent(assignedWork.Id, assignedWork.StudentId));

        if (check.IsComplete)
        {
            MarkAnswersAsChecked(assignedWork);

            assignedWork.CheckedAt = solvedAt;
            assignedWork.CheckStatus = AssignedWorkCheckStatus.CheckedAutomatically;

            await _events.PublishAsync(new CheckedEvent(assignedWork.Id, null));
        }
    }

    public async Task MarkAsCheckedAsync(Ulid assignedWorkId)
    {
        var userId = _currentUser.RequireUserId();

        var assignedWork = await _assignedWorkRepository.GetWithAnswersAsync(
            assignedWorkId,
            userId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsSolved)
        {
            throw new AssignedWorkNotSolvedException();
        }

        if (assignedWork.IsChecked)
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        var checkedAt = Clock.Now;

        MarkAnswersAsChecked(assignedWork);

        // The score the work carried until now is only what could be worked out
        // automatically when it was handed in — nothing at all for a work whose
        // tasks are all checked by hand. What the mentor gave the answers is the
        // score from here on.
        assignedWork.Score = assignedWork.ScoreOfAnswers;

        assignedWork.CheckedAt = checkedAt;
        assignedWork.CheckStatus = AssignedWorkStatuses.CheckedAt(
            checkedAt,
            assignedWork.CheckDeadlineAt
        );

        await _events.PublishAsync(new CheckedEvent(assignedWork.Id, userId));
    }

    public async Task ReturnToSolveAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsSolved)
        {
            throw new AssignedWorkNotSolvedException();
        }

        assignedWork.CheckedAt = null;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.NotChecked;
        assignedWork.Score = null;

        assignedWork.SolvedAt = null;
        assignedWork.SolveStatus = AssignedWorkSolveStatus.InProgress;

        await _events.PublishAsync(
            new SentOnResolveEvent(assignedWork.Id, _currentUser.RequireUserId())
        );
    }

    public async Task ReturnToCheckAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsChecked)
        {
            throw new AssignedWorkNotCheckedException();
        }

        assignedWork.CheckedAt = null;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.NotChecked;

        await _events.PublishAsync(
            new SentOnRecheckEvent(assignedWork.Id, _currentUser.RequireUserId())
        );
    }

    public async Task ShiftDeadlineAsync(
        Ulid assignedWorkId,
        ShiftAssignedWorkDeadlineOptionsDTO options
    )
    {
        var userId = _currentUser.RequireUserId();

        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, userId);

        assignedWork.ThrowNotFoundIfNull();

        switch (_currentUser.UserRole)
        {
            case UserRoles.Student:
                AssertCorrectStudentDeadlineShift(assignedWork, options.NewDeadline);
                assignedWork.SolveDeadlineAt = options.NewDeadline;
                assignedWork.IsSolveDeadlineShifted = true;
                break;
            case UserRoles.Mentor:
                AssertCorrectMentorDeadlineShift(assignedWork, options.NewDeadline);
                assignedWork.CheckDeadlineAt = options.NewDeadline;
                assignedWork.IsCheckDeadlineShifted = true;
                break;
            default:
                throw new ForbiddenException();
        }

        await _events.PublishAsync(
            new DeadlineShiftedEvent(
                assignedWork.Id,
                new ShiftDeadlinePayload
                {
                    NewDeadlineAt = options.NewDeadline,
                    ShiftedByRole = _currentUser.RequireUserRole(),
                    ShiftedById = userId,
                }
            )
        );
    }

    private static void MarkAnswersAsChecked(AssignedWorkModel assignedWork)
    {
        foreach (var answer in assignedWork.Answers)
        {
            answer.Status = AssignedWorkAnswerStatus.Checked;
        }
    }

    /// <summary>
    /// The tasks the student actually got: a remake may leave out the ones already answered
    /// correctly, and those must not count towards the work being fully auto-checkable.
    /// </summary>
    private static IEnumerable<WorkTaskModel> AssignedTasksOf(AssignedWorkModel assignedWork)
    {
        var tasks = assignedWork.Work?.Tasks ?? [];

        return assignedWork.ExcludedTaskIds is { Length: > 0 } excluded
            ? tasks.Where(task => !excluded.Contains(task.Id))
            : tasks;
    }

    private static void AssertCorrectStudentDeadlineShift(
        AssignedWorkModel assignedWork,
        DateTime newDeadline
    )
    {
        if (newDeadline - assignedWork.SolveDeadlineAt > AssignedWorkConfig.MaxSolveDeadlineShift)
        {
            throw new IncorrectDeadlineShiftException();
        }

        if (assignedWork.IsSolved)
        {
            throw new AssignedWorkAlreadySolvedException();
        }
    }

    private static void AssertCorrectMentorDeadlineShift(
        AssignedWorkModel assignedWork,
        DateTime newDeadline
    )
    {
        if (newDeadline - assignedWork.CheckDeadlineAt > AssignedWorkConfig.MaxCheckDeadlineShift)
        {
            throw new IncorrectDeadlineShiftException();
        }

        if (assignedWork.IsChecked)
        {
            throw new AssignedWorkAlreadyCheckedException();
        }
    }
}
