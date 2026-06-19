using AutoFilterer.Types;

namespace Noo.Api.NooTube.Filters;

public class CommentFilter : PaginationFilterBase
{
    public Ulid? VideoId { get; set; }
}
