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

    /// <summary>
    /// The subjects the user has saved tasks on, with how many on each.
    /// </summary>
    public Task<IEnumerable<SavedTaskSubjectDTO>> GetSubjectSummariesAsync(Ulid userId);

    /// <summary>
    /// How many saved tasks the user has, on one subject or across all of them.
    /// </summary>
    public Task<int> CountAsync(Ulid userId, Ulid? subjectId);

    /// <summary>
    /// A random handful of the user's saved tasks, drawn in the database so the
    /// draw does not depend on how many the user has saved.
    /// </summary>
    public Task<IEnumerable<SavedTaskModel>> GetRandomAsync(
        Ulid userId,
        Ulid? subjectId,
        int count
    );

    /// <summary>
    /// One saved task of the user's, with the task it points at loaded — the
    /// answer key included, so it can be checked against.
    /// </summary>
    public Task<SavedTaskModel?> GetWithTaskAsync(Ulid userId, Ulid savedTaskId);
}
