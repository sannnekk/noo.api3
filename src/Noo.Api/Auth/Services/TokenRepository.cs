using Microsoft.EntityFrameworkCore;
using Noo.Api.Auth.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.Auth.Services;

[RegisterScoped(typeof(ITokenRepository))]
public class TokenRepository : Repository<TokenModel>, ITokenRepository
{
    public TokenRepository(NooDbContext context)
        : base(context) { }

    public Task<TokenModel?> GetAsync(string tokenString)
    {
        return Context.GetDbSet<TokenModel>().FirstOrDefaultAsync(t => t.Token == tokenString);
    }

    public void DeleteTokens(Ulid id, TokenType type)
    {
        var tokens = Context.GetDbSet<TokenModel>().Where(t => t.UserId == id && t.Type == type);

        Context.GetDbSet<TokenModel>().RemoveRange(tokens);
    }
}
