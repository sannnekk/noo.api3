using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Support.Models;

namespace Noo.Api.Support.Services;

[RegisterScoped(typeof(ISupportArticleRepository))]
public class SupportArticleRepository : Repository<SupportArticleModel>, ISupportArticleRepository
{
    public SupportArticleRepository(NooDbContext dbContext) : base(dbContext)
    {
    }
}
