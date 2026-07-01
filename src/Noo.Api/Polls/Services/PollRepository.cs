using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using AutoFilterer.Abstractions;
using AutoFilterer.Extensions;
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

    public async Task<SearchResult<PollModel>> SearchWithParticipationsCountAsync(
        IPaginationFilter filter,
        IEnumerable<ISpecification<PollModel>>? specifications = default
    )
    {
        var query = Context.GetDbSet<PollModel>().AsQueryable();

        if (specifications is not null)
        {
            foreach (var specification in specifications)
            {
                query = query.WithSpecification(specification);
            }
        }

        var total = await query.ApplyFilterWithoutPagination(filter).CountAsync();

        var polls = await query.ApplyDefaultOrdering(filter).ApplyFilter(filter).ToListAsync();

        var pollIds = polls.ConvertAll(poll => poll.Id);

        var counts = await Context.GetDbSet<PollParticipationModel>()
            .Where(participation =>
                participation.PollId != null && pollIds.Contains(participation.PollId.Value)
            )
            .GroupBy(participation => participation.PollId!.Value)
            .Select(group => new { PollId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.PollId, entry => entry.Count);

        foreach (var poll in polls)
        {
            poll.ParticipationsCount = counts.GetValueOrDefault(poll.Id);
        }

        return new SearchResult<PollModel>(polls, total);
    }
}
