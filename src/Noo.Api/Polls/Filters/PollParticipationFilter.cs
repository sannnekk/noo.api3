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
    /// Matched against the participant: the user's name, username, e-mail and telegram
    /// username, or the identifier an anonymous participant left. Most of them sit on
    /// the related user, and CompareTo silently drops property paths it cannot resolve
    /// on the filtered entity, so the search is applied by
    /// <see cref="Noo.Api.Polls.Specifications.PollParticipationSearchSpecification"/>.
    /// </summary>
    [IgnoreFilter]
    public string? Search { get; set; }

    public Ulid? PollId { get; set; }

    public ParticipatingUserType? UserType { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }
}
