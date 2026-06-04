using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkHistoryRepository))]
public class AssignedWorkHistoryRepository
    : Repository<AssignedWorkHistoryModel>,
        IAssignedWorkHistoryRepository
{
    public AssignedWorkHistoryRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public async Task<IEnumerable<AssignedWorkHistoryModel>> GetHistoryAsync(Ulid assignedWorkId)
    {
        return await Context
            .Set<AssignedWorkHistoryModel>()
            .Where(x => x.AssignedWorkId == assignedWorkId)
            .ToListAsync();
    }
}
