using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkAnswerRepository))]
public class AssignedWorkAnswerRepository : Repository<AssignedWorkAnswerModel>, IAssignedWorkAnswerRepository
{
    public AssignedWorkAnswerRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Ulid[]> GetCorrectAnswerIdsAsync(Ulid assignedWorkId)
    {
        return Context.Set<AssignedWorkAnswerModel>()
            .Where(a => a.AssignedWorkId == assignedWorkId)
            .Where(a => a.Score == a.MaxScore)
            .Select(a => a.TaskId)
            .ToArrayAsync();
    }
}
