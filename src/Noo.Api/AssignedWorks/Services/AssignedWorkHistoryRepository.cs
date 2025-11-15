using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkHistoryRepository))]
public class AssignedWorkHistoryRepository : Repository<AssignedWorkStatusHistoryModel>, IAssignedWorkHistoryRepository
{
    public AssignedWorkHistoryRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<AssignedWorkStatusHistoryModel>> GetHistoryAsync(Ulid assignedWorkId)
    {
        return await Context.Set<AssignedWorkStatusHistoryModel>()
            .Where(x => x.AssignedWorkId == assignedWorkId)
            .ToListAsync();
    }
}
