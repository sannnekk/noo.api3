using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.SavedTasks.Models;

namespace Noo.Api.SavedTasks.Filters;

[PossibleSortings(nameof(SavedTaskModel.CreatedAt))]
public class SavedTaskFilter : PaginationFilterBase
{
    /// <summary>
    /// Matched against the work's title and its subject's name. Both sit behind
    /// the task rather than on the saved task itself, and CompareTo only
    /// resolves properties of the filtered entity, so the search is applied by
    /// <see cref="Noo.Api.SavedTasks.Specifications.SavedTaskSpecification"/>.
    /// </summary>
    [IgnoreFilter]
    public string? Search { get; set; }
}
