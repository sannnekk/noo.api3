using Noo.Api.Auth.Models;
using Noo.Api.Core.DataAbstraction.Db;

namespace Noo.Api.Auth.Services;

public interface ITokenRepository : IRepository<TokenModel>
{
    public void DeleteTokens(Ulid id, TokenType passwordReset);
    public Task<TokenModel?> GetAsync(string tokenString);
}
