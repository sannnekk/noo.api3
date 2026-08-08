using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.UserHistory.Models;

namespace Noo.Api.UserHistory.Services;

[RegisterScoped(typeof(IUserHistoryRepository))]
public class UserHistoryRepository : Repository<UserHistoryModel>, IUserHistoryRepository
{
    public UserHistoryRepository(NooDbContext dbContext)
        : base(dbContext) { }
}
