using AutoFilterer.Attributes;
using AutoFilterer.Types;

namespace Noo.Api.MediaDownloads.Filters;

/// <summary>
/// Paging for the per-user download breakdown.
/// </summary>
/// <remarks>
/// Derives from <see cref="PaginationFilterBase"/> only so the client's usual <c>page</c> /
/// <c>perPage</c> parameters bind. The query groups by user, so <c>ApplyFilter</c> never runs over
/// it and every property here is applied by hand.
/// </remarks>
public class MaterialFileDownloadsFilter : PaginationFilterBase
{
    /// <summary>
    /// Narrows the breakdown to a single attached file. Omitted, it covers every file of the material.
    /// </summary>
    [IgnoreFilter]
    public Ulid? MediaId { get; set; }
}
