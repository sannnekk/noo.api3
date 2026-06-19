using Ardalis.Specification;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Specifications;

public class CommentSpecification : Specification<NooTubeVideoCommentModel>
{
    public CommentSpecification()
    {
        Query.Include(c => c.User);
    }
}
