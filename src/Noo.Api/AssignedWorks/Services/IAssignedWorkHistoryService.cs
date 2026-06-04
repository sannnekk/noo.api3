using Noo.Api.AssignedWorks.Models;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkHistoryService
{
    public Task<IEnumerable<AssignedWorkHistoryModel>> GetHistoryAsync(Ulid assignedWorkId);

    public void PushEvent(AssignedWorkHistoryModel @event);
}
