using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

public class PollRepository : Repository<PollModel>, IPollRepository
{
    public Task<PollModel?> GetWithQuestionsAsync(Ulid pollId)
    {
        return Context.Set<PollModel>()
            .Where(p => p.Id == pollId)
            .Include(p => p.Questions.OrderBy(q => q.Order))
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
}


public static class IUnitOfWorkPollRepositoryExtension
{
    public static IPollRepository PollRepository(this IUnitOfWork unitOfWork)
    {
        return new PollRepository()
        {
            Context = unitOfWork.Context,
        };
    }
}
