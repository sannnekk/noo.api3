using Noo.Api.AssignedWorks.DTO;

namespace Noo.Api.AssignedWorks.Services;

/// <summary>
/// The moves a work makes through solving and checking, and the deadlines those moves
/// are measured against.
/// </summary>
public interface IAssignedWorkLifecycleService
{
    public Task MarkAsSolvedAsync(Ulid assignedWorkId);
    public Task MarkAsCheckedAsync(Ulid assignedWorkId);
    public Task ReturnToSolveAsync(Ulid assignedWorkId);
    public Task ReturnToCheckAsync(Ulid assignedWorkId);
    public Task ShiftDeadlineAsync(
        Ulid assignedWorkId,
        ShiftAssignedWorkDeadlineOptionsDTO options
    );
}
