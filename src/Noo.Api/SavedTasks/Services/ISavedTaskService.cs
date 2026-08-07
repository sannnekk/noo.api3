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
}
