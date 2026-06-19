using AutoFilterer.Attributes;
using AutoFilterer.Types;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Filters;

[PossibleSortings(nameof(NooTubeVideoModel.Title), nameof(NooTubeVideoModel.CreatedAt))]
public class VideoFilter : PaginationFilterBase
{
    [CompareTo(nameof(NooTubeVideoModel.Title))]
    [ToLowerContainsComparison]
    public string? Search { get; set; }

    public DateTime? CreatedAt { get; set; }

    [IgnoreFilter]
    public VideoFilterType Type { get; set; } = VideoFilterType.All;
}
