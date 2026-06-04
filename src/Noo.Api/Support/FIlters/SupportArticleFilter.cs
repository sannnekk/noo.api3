using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Support.Models;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.Filters;

[PossibleSortings(nameof(SupportArticleModel.Order))]
public class SupportArticleFilter : PaginationFilterBase
{
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(SupportArticleModel.Title))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public SupportCategory? Category { get; set; } = SupportCategory.Courses;
}
