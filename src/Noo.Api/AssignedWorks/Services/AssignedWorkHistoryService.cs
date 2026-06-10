using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Specifications;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkHistoryService))]
public class AssignedWorkHistoryService : IAssignedWorkHistoryService
{
    private readonly IAssignedWorkHistoryRepository _assignedWorkHistoryRepository;

    public AssignedWorkHistoryService(IAssignedWorkHistoryRepository assignedWorkHistoryRepository)
    {
        _assignedWorkHistoryRepository = assignedWorkHistoryRepository;
    }

    public Task<SearchResult<AssignedWorkHistoryModel>> GetHistoryAsync(
        Ulid assignedWorkId,
        AssignedWorkHistoryFilter filter
    )
    {
        filter.AssignedWorkId = assignedWorkId;

        return _assignedWorkHistoryRepository.SearchAsync(filter, [new HistorySpecification()]);
    }
}
