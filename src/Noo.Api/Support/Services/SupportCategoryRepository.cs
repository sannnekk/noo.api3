using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Support.Models;

namespace Noo.Api.Support.Services;

[RegisterScoped(typeof(ISupportCategoryRepository))]
public class SupportCategoryRepository : Repository<SupportCategoryModel>, ISupportCategoryRepository
{
    public SupportCategoryRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<SupportCategoryModel>> GetCategoryTreeAsync(bool includeInactive = false)
    {
        return await Context.GetDbSet<SupportCategoryModel>()
            .Where(c => c.ParentId == null)
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.Order)
            .Include(c => c.Children)
            .Include(c => c.Articles)
            .AsNoTracking()
            .ToListAsync();
    }
}
