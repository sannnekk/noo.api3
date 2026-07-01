using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.Filters;

[PossibleSortings(
    nameof(WorkModel.Title),
    nameof(WorkModel.Type),
    nameof(WorkModel.SubjectId),
    nameof(WorkModel.CreatedAt)
)]
public class WorkFilter : PaginationFilterBase
{
    [CompareTo(nameof(WorkModel.Title))]
    [CompareTo(nameof(WorkModel.Description))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    [ArraySearchFilter]
    public IEnumerable<WorkType>? Type { get; set; }

    [ArraySearchFilter]
    public IEnumerable<Ulid?>? SubjectId { get; set; }

    public Range<DateTime>? CreatedAt { get; set; }
}
