using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.Filters;

[PossibleSortings(
    nameof(PollParticipationModel.PollId),
    nameof(PollParticipationModel.UserType),
    nameof(PollParticipationModel.CreatedAt)
)]
public class PollParticipationFilter : PaginationFilterBase
{
    /// <summary>
    /// Matched against whatever identifies a participation in the list being read: the
    /// participant when the list is a poll's results, the poll when it is a user's own
    /// participations. Both sit on a related entity, and CompareTo silently drops
    /// property paths it cannot resolve on the filtered entity, so the search is applied
    /// by the specification of the respective list.
    /// </summary>
    [IgnoreFilter]
    public string? Search { get; set; }

    public Ulid? PollId { get; set; }

    public ParticipatingUserType? UserType { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }
}
