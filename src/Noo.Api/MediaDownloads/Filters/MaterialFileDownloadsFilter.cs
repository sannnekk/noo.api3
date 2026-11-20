using AutoFilterer.Attributes;
using AutoFilterer.Types;

namespace Noo.Api.MediaDownloads.Filters;

public class MaterialFileDownloadsFilter : PaginationFilterBase
{
    [IgnoreFilter]
    public Ulid? MediaId { get; set; }
}
