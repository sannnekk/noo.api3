using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Types;

namespace Noo.Api.GoogleSheetsIntegrations.Filters;

[PossibleSortings(
    nameof(GoogleSheetsIntegrationModel.Name),
    nameof(GoogleSheetsIntegrationModel.CreatedAt),
    nameof(GoogleSheetsIntegrationModel.LastRunAt)
)]
public class GoogleSheetsIntegrationFilter : PaginationFilterBase
{
    [CompareTo(nameof(GoogleSheetsIntegrationModel.Name))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    [ArraySearchFilter]
    public IEnumerable<GoogleSheetsIntegrationType>? Type { get; set; }

    [ArraySearchFilter]
    public IEnumerable<GoogleSheetsIntegrationStatus>? Status { get; set; }

    public Range<DateTime>? LastRunAt { get; set; }
}
