using Noo.Api.AssignedWorks.DTO;

namespace Noo.Api.AssignedWorks.Services;

/// <summary>
/// What a student may do with one task of a work while they are still solving it, where
/// the task allows it: look up its answer key, or have it checked on its own.
/// </summary>
public interface IAssignedWorkTaskService
{
    public Task<AssignedWorkTaskAnswerKeyDTO> GetAnswerKeyAsync(
        Ulid assignedWorkId,
        Ulid taskId
    );

    public Task<AssignedWorkTaskCheckDTO> CheckAsync(Ulid assignedWorkId, Ulid taskId);
}
