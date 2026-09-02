using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Support.Models;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.Filters;

[PossibleSortings(nameof(SupportFaqItemModel.Order))]
public class SupportFaqItemFilter : PaginationFilterBase
{
    [CompareTo(nameof(SupportFaqItemModel.Question))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    /// <summary>
    /// Unset by default, unlike the article filter: the home page wants every
    /// item, whatever category it belongs to.
    /// </summary>
    public SupportCategory? Category { get; set; }

    public bool? IsActive { get; set; }
}
