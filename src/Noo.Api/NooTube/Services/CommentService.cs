using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Specifications;
using SystemTextJsonPatch;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(ICommentService))]
public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;

    private readonly IJsonPatchUpdateService _patchService;

    private readonly ICurrentUser _currentUser;

    private readonly IMapper _mapper;

    public CommentService(
        ICommentRepository commentRepository,
        IJsonPatchUpdateService pathcService,
        ICurrentUser currentUser,
        IMapper mapper
    )
    {
        _commentRepository = commentRepository;
        _patchService = pathcService;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task DeleteCommentAsync(Ulid commentId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);

        if (comment is null)
        {
            return;
        }

        var userRole = _currentUser.RequireUserRole();
        var userId = _currentUser.RequireUserId();

        if (comment.UserId == userId)
        {
            _commentRepository.Delete(comment);
            return;
        }

        if (userRole == UserRoles.Admin || userRole == UserRoles.Teacher)
        {
            _commentRepository.Delete(comment);
            return;
        }
    }

    public Task<SearchResult<NooTubeVideoCommentModel>> GetAsync(CommentFilter filter)
    {
        return _commentRepository.SearchAsync(filter, [new CommentSpecification()]);
    }

    public void CreateComment(Ulid videoId, CreateNooTubeVideoCommentDTO comment)
    {
        var commentModel = _mapper.Map<NooTubeVideoCommentModel>(comment);

        var userId = _currentUser.RequireUserId();
        commentModel.UserId = userId;
        commentModel.VideoId = videoId;

        _commentRepository.Add(commentModel);
    }

    public async Task UpdateAsync(
        Ulid commentId,
        JsonPatchDocument<UpdateNooTubeVideoCommentDTO> patch
    )
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);

        comment.ThrowNotFoundIfNull();

        var userRole = _currentUser.RequireUserRole();
        var userId = _currentUser.RequireUserId();

        if (comment.UserId == userId)
        {
            _patchService.ApplyPatch(comment, patch);
            return;
        }

        if (userRole == UserRoles.Admin || userRole == UserRoles.Teacher)
        {
            _patchService.ApplyPatch(comment, patch);
            return;
        }
    }
}
