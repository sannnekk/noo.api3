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
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(PollParticipationModel.User.Name))]
    [CompareTo(nameof(PollParticipationModel.User.Username))]
    [CompareTo(nameof(PollParticipationModel.User.TelegramUsername))]
    [CompareTo(nameof(PollParticipationModel.User.Email))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public Ulid? PollId { get; set; }

    public ParticipatingUserType? UserType { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }
}
