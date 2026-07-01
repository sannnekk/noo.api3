using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Polls.Models;

namespace Noo.Api.Polls.Filters;

[PossibleSortings(
    nameof(PollModel.Title),
    nameof(PollModel.Description),
    nameof(PollModel.CreatedAt),
    nameof(PollModel.UpdatedAt),
    nameof(PollModel.IsActive),
    nameof(PollModel.IsAuthRequired)
)]
public class PollFilter : PaginationFilterBase
{
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(PollModel.Title))]
    [CompareTo(nameof(PollModel.Description))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }

    public Range<DateTime>? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsAuthRequired { get; set; }
}
