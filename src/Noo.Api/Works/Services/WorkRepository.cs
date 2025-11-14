using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.Models;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkRepository))]
public class WorkRepository : Repository<WorkModel>, IWorkRepository
{
    public WorkRepository(NooDbContext context) : base(context)
    {
    }

    public Task<WorkModel?> GetWithTasksAsync(Ulid id)
    {
        var repository = Context.GetDbSet<WorkModel>();

        return repository
            .Include(x => x.Tasks!
                .OrderBy(task => task.Order))
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
