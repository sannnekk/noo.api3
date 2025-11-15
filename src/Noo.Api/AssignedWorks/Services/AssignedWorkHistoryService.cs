using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkHistoryService))]
public class AssignedWorkHistoryService : IAssignedWorkHistoryService
{
    private readonly IAssignedWorkHistoryRepository _assignedWorkHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignedWorkHistoryService(IUnitOfWork unitOfWork, IAssignedWorkHistoryRepository assignedWorkHistoryRepository)
    {
        _unitOfWork = unitOfWork;
        _assignedWorkHistoryRepository = assignedWorkHistoryRepository;
    }

    public Task<IEnumerable<AssignedWorkStatusHistoryModel>> GetHistoryAsync(Ulid assignedWorkId)
    {
        return _assignedWorkHistoryRepository.GetHistoryAsync(assignedWorkId);
    }

    public Task PushEventAsync(AssignedWorkStatusHistoryModel @event)
    {
        _assignedWorkHistoryRepository.Add(@event);
        return _unitOfWork.CommitAsync();
    }
}
