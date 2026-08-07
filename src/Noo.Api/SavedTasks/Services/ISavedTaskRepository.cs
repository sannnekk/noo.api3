using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.SavedTasks.DTO;
using Noo.Api.SavedTasks.Models;

namespace Noo.Api.SavedTasks.Services;

public interface ISavedTaskRepository : IRepository<SavedTaskModel>
{
    public Task<SavedTaskModel?> GetAsync(Ulid userId, Ulid taskId);

    /// <summary>
    /// The user's saved tasks as bare references, narrowed to the ones saved
    /// from a single assigned work when one is given.
    /// </summary>
    public Task<IEnumerable<SavedTaskReferenceDTO>> GetReferencesAsync(
        Ulid userId,
        Ulid? assignedWorkId
    );

    /// <summary>
    /// The assigned work a student may save the given task from, or <c>null</c>
    /// when the work is not theirs or the task is not part of it. Whether the
    /// work has been checked is left for the caller to decide on.
    /// </summary>
    public Task<AssignedWorkModel?> GetSavableWorkAsync(
        Ulid studentId,
        Ulid assignedWorkId,
        Ulid taskId
    );
}
