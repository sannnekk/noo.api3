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
}
