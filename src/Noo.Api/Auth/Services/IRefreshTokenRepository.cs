using Noo.Api.Auth.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Auth.Services;

public interface IRefreshTokenRepository : IRepository<RefreshTokenModel>
{
    public Task<RefreshTokenModel?> GetByHashAsync(string tokenHash);
    public void DeleteForSession(Ulid sessionId);
}
