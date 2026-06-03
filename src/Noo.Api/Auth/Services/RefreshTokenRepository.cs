using Microsoft.EntityFrameworkCore;
using Noo.Api.Auth.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(IRefreshTokenRepository))]
public class RefreshTokenRepository : Repository<RefreshTokenModel>, IRefreshTokenRepository
{
    public RefreshTokenRepository(NooDbContext context)
        : base(context) { }

    public Task<RefreshTokenModel?> GetByHashAsync(string tokenHash)
    {
        return Context.GetDbSet<RefreshTokenModel>()
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public void DeleteForSession(Ulid sessionId)
    {
        var tokens = Context.GetDbSet<RefreshTokenModel>().Where(t => t.SessionId == sessionId);

        Context.GetDbSet<RefreshTokenModel>().RemoveRange(tokens);
    }
}
