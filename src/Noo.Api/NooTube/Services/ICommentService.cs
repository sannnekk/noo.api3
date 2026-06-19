using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using SystemTextJsonPatch;

namespace Noo.Api.NooTube.Services;

public interface ICommentService
{
    public Task<SearchResult<NooTubeVideoCommentModel>> GetAsync(CommentFilter filter);

    public void CreateComment(Ulid videoId, CreateNooTubeVideoCommentDTO comment);

    public Task UpdateAsync(Ulid commentId, JsonPatchDocument<UpdateNooTubeVideoCommentDTO> patch);

    public Task DeleteCommentAsync(Ulid commentId);
}
