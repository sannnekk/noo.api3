using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Works.Models;

namespace Noo.Api.Works.Services;

public class WorkRepository : Repository<WorkModel>, IWorkRepository
{
    public Task<WorkModel?> GetWithTasksAsync(Ulid id)
    {
        var repository = Context.GetDbSet<WorkModel>();

        return repository
            .Include(x => x.Tasks!
                .OrderBy(task => task.Order))
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}

public static class UnitIOfWorkWorkRepositoryExtensions
{
    public static IWorkRepository WorkRepository(this IUnitOfWork unitOfWork)
    {
        return new WorkRepository()
        {
            Context = unitOfWork.Context
        };
    }
}
