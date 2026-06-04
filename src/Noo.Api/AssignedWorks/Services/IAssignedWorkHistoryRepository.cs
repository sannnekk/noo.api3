using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkHistoryRepository : IRepository<AssignedWorkHistoryModel>
{
    public Task<IEnumerable<AssignedWorkHistoryModel>> GetHistoryAsync(Ulid assignedWorkId);
}

