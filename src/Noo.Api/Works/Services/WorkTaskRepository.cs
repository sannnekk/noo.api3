using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.Models;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkTaskRepository))]
public class WorkTaskRepository : Repository<WorkTaskModel>, IWorkTaskRepository
{
    public WorkTaskRepository(NooDbContext context)
        : base(context) { }

    public Task<int> GetWorkMaxScoreAsync(Ulid workId)
    {
        return Context
            .GetDbSet<WorkTaskModel>()
            .Where(t => t.WorkId == workId)
            .SumAsync(t => t.MaxScore);
    }
}
