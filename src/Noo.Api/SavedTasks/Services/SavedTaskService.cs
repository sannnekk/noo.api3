using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.SavedTasks.DTO;
using Noo.Api.SavedTasks.Exceptions;
using Noo.Api.SavedTasks.Filters;
using Noo.Api.SavedTasks.Models;
using Noo.Api.SavedTasks.Specifications;

namespace Noo.Api.SavedTasks.Services;

[RegisterScoped(typeof(ISavedTaskService))]
public class SavedTaskService : ISavedTaskService
{
    private readonly ISavedTaskRepository _savedTaskRepository;

    private readonly ITaskCheckService _taskCheckService;

    private readonly ICurrentUser _currentUser;

    public SavedTaskService(
        ISavedTaskRepository savedTaskRepository,
        ITaskCheckService taskCheckService,
        ICurrentUser currentUser
    )
    {
        _savedTaskRepository = savedTaskRepository;
        _taskCheckService = taskCheckService;
        _currentUser = currentUser;
    }

    public async Task<Ulid> CreateSavedTaskAsync(CreateSavedTaskDTO createSavedTaskDTO)
    {
        var userId = _currentUser.RequireUserId();

        // Saving is only allowed through an assigned work of the student's own,
        // and only once it is checked. Without that the endpoint would hand out
        // the answers and explanations of any task in the system.
        var assignedWork = await _savedTaskRepository.GetSavableWorkAsync(
            userId,
            createSavedTaskDTO.AssignedWorkId,
            createSavedTaskDTO.TaskId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsChecked)
        {
            throw new AssignedWorkNotCheckedException();
        }

        var alreadySaved = await _savedTaskRepository.GetAsync(userId, createSavedTaskDTO.TaskId);

        if (alreadySaved != null)
        {
            throw new AlreadyExistsException();
        }

        var savedTask = new SavedTaskModel
        {
            UserId = userId,
            TaskId = createSavedTaskDTO.TaskId,
            AssignedWorkId = createSavedTaskDTO.AssignedWorkId,
        };

        _savedTaskRepository.Add(savedTask);

        return savedTask.Id;
    }

    public Task<SearchResult<SavedTaskModel>> GetSavedTasksAsync(SavedTaskFilter filter)
    {
        var specification = new SavedTaskSpecification(
            _currentUser.RequireUserId(),
            filter.Search,
            filter.SubjectId
        );

        return _savedTaskRepository.SearchAsync(filter, [specification]);
    }

    public Task<IEnumerable<SavedTaskReferenceDTO>> GetReferencesAsync(Ulid? assignedWorkId)
    {
        return _savedTaskRepository.GetReferencesAsync(_currentUser.RequireUserId(), assignedWorkId);
    }

    public async Task DeleteSavedTaskAsync(Ulid savedTaskId)
    {
        var savedTask = await _savedTaskRepository.GetByIdAsync(savedTaskId);

        if (savedTask == null || savedTask.UserId != _currentUser.RequireUserId())
        {
            return;
        }

        _savedTaskRepository.Delete(savedTask);
    }

    public Task<IEnumerable<SavedTaskSubjectDTO>> GetSubjectSummariesAsync()
    {
        return _savedTaskRepository.GetSubjectSummariesAsync(_currentUser.RequireUserId());
    }

    public async Task<IEnumerable<SavedTaskModel>> GetQuizDeckAsync(Ulid? subjectId, int count)
    {
        var userId = _currentUser.RequireUserId();
        var available = await _savedTaskRepository.CountAsync(userId, subjectId);

        if (available < SavedTaskConfig.MinQuizCardCount)
        {
            throw new NotEnoughSavedTasksException();
        }

        // A deck of every card the subject holds is still a deck; asking for
        // more than that is not an error, it just runs out.
        var deckSize = Math.Clamp(count, SavedTaskConfig.MinQuizCardCount, available);

        return await _savedTaskRepository.GetRandomAsync(userId, subjectId, deckSize);
    }

    public async Task<SavedTaskAnswerCheckDTO> CheckAnswerAsync(
        Ulid savedTaskId,
        CheckSavedTaskAnswerDTO checkAnswerDto
    )
    {
        var savedTask = await _savedTaskRepository.GetWithTaskAsync(
            _currentUser.RequireUserId(),
            savedTaskId
        );

        savedTask.ThrowNotFoundIfNull();

        var score = _taskCheckService.CheckWord(savedTask.Task, checkAnswerDto.Answer);

        if (score == null)
        {
            throw new BadRequestException("This task cannot be checked automatically.");
        }

        return new SavedTaskAnswerCheckDTO
        {
            Score = score.Value,
            MaxScore = savedTask.Task.MaxScore,
            IsCorrect = score.Value >= savedTask.Task.MaxScore,
        };
    }
}
