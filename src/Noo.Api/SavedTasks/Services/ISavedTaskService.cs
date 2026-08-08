using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.SavedTasks.DTO;
using Noo.Api.SavedTasks.Filters;
using Noo.Api.SavedTasks.Models;

namespace Noo.Api.SavedTasks.Services;

public interface ISavedTaskService
{
    public Task<Ulid> CreateSavedTaskAsync(CreateSavedTaskDTO createSavedTaskDTO);
    public Task<SearchResult<SavedTaskModel>> GetSavedTasksAsync(SavedTaskFilter filter);
    public Task<IEnumerable<SavedTaskReferenceDTO>> GetReferencesAsync(Ulid? assignedWorkId);
    public Task DeleteSavedTaskAsync(Ulid savedTaskId);

    /// <summary>
    /// The subjects the current student can run a quiz on, with how many cards
    /// each holds.
    /// </summary>
    public Task<IEnumerable<SavedTaskSubjectDTO>> GetSubjectSummariesAsync();

    /// <summary>
    /// A random deck for a quiz. Throws when the subject holds fewer than
    /// <see cref="SavedTaskConfig.MinQuizCardCount"/> cards.
    /// </summary>
    public Task<IEnumerable<SavedTaskModel>> GetQuizDeckAsync(Ulid? subjectId, int count);

    /// <summary>
    /// Scores an answer to one saved task with the checker the work it came from
    /// was scored by. Throws when the task is not one that can be checked
    /// automatically.
    /// </summary>
    public Task<SavedTaskAnswerCheckDTO> CheckAnswerAsync(
        Ulid savedTaskId,
        CheckSavedTaskAnswerDTO checkAnswerDto
    );
}
