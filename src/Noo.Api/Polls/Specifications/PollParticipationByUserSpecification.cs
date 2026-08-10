using Ardalis.Specification;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Specifications;

public class PollParticipationByUserSpecification : Specification<PollParticipationModel>
{
    public PollParticipationByUserSpecification(Ulid userId, string? search = null)
    {
        Query.Where(participation => participation.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            Query.Where(participation => participation.Poll.Title.ToLower().Contains(term));
        }

        Query.Include(participation => participation.Poll);
    }
}
