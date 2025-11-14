using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Snippets.Models;

namespace Noo.Api.Snippets.Services;

[RegisterScoped(typeof(ISnippetRepository))]
public class SnippetRepository : Repository<SnippetModel>, ISnippetRepository
{
    public SnippetRepository(NooDbContext context) : base(context)
    {
    }

    public Task<SnippetModel?> GetAsync(Ulid snippetId, Ulid userId)
    {
        return Context.GetDbSet<SnippetModel>()
            .Where(x => x.Id == snippetId && x.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
