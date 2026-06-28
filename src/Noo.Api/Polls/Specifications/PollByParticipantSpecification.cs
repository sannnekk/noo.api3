using Ardalis.Specification;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Specifications;

public class PollByParticipantSpecification : Specification<PollModel>
{
    public PollByParticipantSpecification(Ulid userId)
    {
        Query.Where(poll =>
            poll.Participations.Any(participation => participation.UserId == userId)
        );
    }
}
