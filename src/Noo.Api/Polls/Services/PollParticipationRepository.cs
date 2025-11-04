using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Services;

public class PollParticipationRepository : Repository<PollParticipationModel>, IPollParticipationRepository
{
    public Task<List<PollParticipationModel>> GetByPollIdAsync(Ulid pollId)
    {
        return Context.Set<PollParticipationModel>()
            .AsNoTracking()
            .Where(p => p.PollId == pollId)
            .Include(p => p.User)
            .Include(p => p.Answers)
            .ToListAsync();
    }

    public Task<bool> ParticipationExistsAsync(Ulid pollId, Ulid? userId, string? userExternalIdentifier)
    {
        var query = Context.Set<PollParticipationModel>()
            .AsNoTracking()
            .Where(p => p.PollId == pollId);

        if (userId.HasValue)
        {
            return query.AnyAsync(p => p.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(userExternalIdentifier))
        {
            return query.AnyAsync(p => p.UserExternalIdentifier == userExternalIdentifier);
        }

        // No identifier provided; treat as no duplicate
        return Task.FromResult(false);
    }
}

public static class IUnitOfWorkPollParticipationRepositoryExtension
{
    public static IPollParticipationRepository PollParticipationRepository(this IUnitOfWork unitOfWork)
    {
        return new PollParticipationRepository()
        {
            Context = unitOfWork.Context,
        };
    }
}
