using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

[RegisterScoped(typeof(IPollAnswerRepository))]
public class PollAnswerRepository : Repository<PollAnswerModel>, IPollAnswerRepository
{
    public PollAnswerRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<PollAnswerModel?> GetForUpdateAsync(Ulid answerId)
    {
        return Context.Set<PollAnswerModel>()
            .Include(answer => answer.PollQuestion)
            .Include(answer => answer.Medias)
            .FirstOrDefaultAsync(answer => answer.Id == answerId);
    }
}
