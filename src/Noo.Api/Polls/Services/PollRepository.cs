using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

[RegisterScoped(typeof(IPollRepository))]
public class PollRepository : Repository<PollModel>, IPollRepository
{
    public PollRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<PollModel?> GetWithQuestionsAsync(Ulid pollId)
    {
        return Context.Set<PollModel>()
            .Where(p => p.Id == pollId)
            .Include(p => p.Questions.OrderBy(q => q.Order))
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
}
