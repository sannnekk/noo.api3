using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkHistoryService
{
    public Task<SearchResult<AssignedWorkHistoryModel>> GetHistoryAsync(
        Ulid assignedWorkId,
        AssignedWorkHistoryFilter filter
    );
}
