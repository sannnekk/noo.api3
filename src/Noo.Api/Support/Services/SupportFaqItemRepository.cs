using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Support.Models;

namespace Noo.Api.Support.Services;

[RegisterScoped(typeof(ISupportFaqItemRepository))]
public class SupportFaqItemRepository
    : Repository<SupportFaqItemModel>,
        ISupportFaqItemRepository
{
    public SupportFaqItemRepository(NooDbContext dbContext)
        : base(dbContext) { }
}
