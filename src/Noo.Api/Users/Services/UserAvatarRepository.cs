using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IUserAvatarRepository))]
public class UserAvatarRepository : Repository<UserAvatarModel>, IUserAvatarRepository
{
    public UserAvatarRepository(NooDbContext context)
        : base(context) { }

    public Task<UserAvatarModel?> GetUserAvatarByUserIdAsync(Ulid userId)
    {
        return Context.GetDbSet<UserAvatarModel>().FirstOrDefaultAsync(x => x.UserId == userId);
    }
}
