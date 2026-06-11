using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

public interface IUserAvatarRepository : IRepository<UserAvatarModel>
{
    public Task<UserAvatarModel?> GetUserAvatarByUserIdAsync(Ulid userId);
}
