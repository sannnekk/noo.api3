using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkHistoryService))]
public class AssignedWorkHistoryService : IAssignedWorkHistoryService
{
    private readonly IAssignedWorkHistoryRepository _assignedWorkHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignedWorkHistoryService(
        IUnitOfWork unitOfWork,
        IAssignedWorkHistoryRepository assignedWorkHistoryRepository
    )
    {
        _unitOfWork = unitOfWork;
        _assignedWorkHistoryRepository = assignedWorkHistoryRepository;
    }

    public Task<IEnumerable<AssignedWorkHistoryModel>> GetHistoryAsync(Ulid assignedWorkId)
    {
        return _assignedWorkHistoryRepository.GetHistoryAsync(assignedWorkId);
    }

    public void PushEvent(AssignedWorkHistoryModel @event)
    {
        _assignedWorkHistoryRepository.Add(@event);
    }
}
