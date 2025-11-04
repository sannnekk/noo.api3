using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.GoogleSheetsIntegrations.Models;

namespace Noo.Api.GoogleSheetsIntegrations.Filters;

[PossibleSortings(
    nameof(GoogleSheetsIntegrationModel.Name),
    nameof(GoogleSheetsIntegrationModel.CreatedAt),
    nameof(GoogleSheetsIntegrationModel.LastRunAt)
)]
public class GoogleSheetsIntegrationFilter : PaginationFilterBase
{
    // 2) Global Search: one field that compares to multiple props
    [CompareTo(nameof(GoogleSheetsIntegrationModel.Name))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public string? Entity { get; set; }

    public DateTime? LastRunAt { get; set; }
}
