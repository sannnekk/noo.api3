using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.Models;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkTaskService))]
public class AssignedWorkTaskService : IAssignedWorkTaskService
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly ITaskCheckService _taskCheckService;
    private readonly ICurrentUser _currentUser;

    public AssignedWorkTaskService(
        IAssignedWorkRepository assignedWorkRepository,
        ITaskCheckService taskCheckService,
        ICurrentUser currentUser
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _taskCheckService = taskCheckService;
        _currentUser = currentUser;
    }

    public async Task<AssignedWorkTaskAnswerKeyDTO> GetAnswerKeyAsync(
        Ulid assignedWorkId,
        Ulid taskId
    )
    {
        var (_, task) = await GetOwnWorkAndTaskAsync(assignedWorkId, taskId);

        // The key is the task's to give away. Asking for it on a task that does not
        // offer it is refused rather than answered with nothing, so the client cannot
        // tell an empty key from a withheld one.
        if (!task.ShowAnswerBeforeCheck)
        {
            throw new TaskAnswerKeyNotOfferedException();
        }

        return new AssignedWorkTaskAnswerKeyDTO
        {
            TaskId = task.Id,
            RightAnswers = task.RightAnswers ?? [],
        };
    }

    public async Task<AssignedWorkTaskCheckDTO> CheckAsync(Ulid assignedWorkId, Ulid taskId)
    {
        var (assignedWork, task) = await GetOwnWorkAndTaskAsync(assignedWorkId, taskId);

        if (!task.CheckOneByOne)
        {
            throw new TaskNotCheckableOnItsOwnException();
        }

        // Once the work has been handed in there is nothing left to check one task at a
        // time — the whole of it is being checked.
        if (assignedWork.IsSolved)
        {
            throw new AssignedWorkAlreadySolvedException();
        }

        var answer =
            assignedWork.Answers.FirstOrDefault(a => a.TaskId == taskId)
            ?? throw new TaskNotAnsweredException();

        // Checking twice is the same as checking once: the verdict already given stands,
        // so a double click cannot rescore an answer the student can no longer change.
        if (answer.Status == AssignedWorkAnswerStatus.Checked)
        {
            return VerdictOf(task, answer, answer.Score ?? 0);
        }

        var score =
            _taskCheckService.CheckWord(task, answer.WordContent)
            ?? throw new TaskNotCheckableOnItsOwnException();

        answer.Score = score;
        answer.Status = AssignedWorkAnswerStatus.Checked;

        return VerdictOf(task, answer, score);
    }

    private static AssignedWorkTaskCheckDTO VerdictOf(
        WorkTaskModel task,
        AssignedWorkAnswerModel answer,
        int score
    ) =>
        new()
        {
            TaskId = task.Id,
            AnswerId = answer.Id,
            Score = score,
            MaxScore = task.MaxScore,
            IsCorrect = score >= task.MaxScore,
        };

    /// <summary>
    /// The work as one of the caller's own, together with the task they named. Both of
    /// these operations are the student's while they solve, so a work belonging to
    /// somebody else — or a task belonging to another work — is simply not found.
    /// </summary>
    private async Task<(AssignedWorkModel, WorkTaskModel)> GetOwnWorkAndTaskAsync(
        Ulid assignedWorkId,
        Ulid taskId
    )
    {
        var assignedWork = await _assignedWorkRepository.GetWithAnswersAndTasksAsync(
            assignedWorkId
        );

        assignedWork.ThrowNotFoundIfNull();

        // Same reading as the solve flow: a work that is not this student's own is not
        // theirs to know about either.
        if (assignedWork.StudentId != _currentUser.UserId)
        {
            throw new NotFoundException();
        }

        var task =
            (assignedWork.Work?.Tasks ?? []).FirstOrDefault(t => t.Id == taskId)
            ?? throw new NotFoundException();

        return (assignedWork, task);
    }
}
