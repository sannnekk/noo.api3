using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.NooTube.DTO;
using Noo.Api.NooTube.Filters;
using Noo.Api.NooTube.Models;
using Noo.Api.NooTube.Services;
using Noo.Api.NooTube.Types;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.NooTube;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("nootube")]
public class NootubeController : ApiController
{
    private readonly IVideoService _videoService;

    private readonly ICommentService _commentService;

    public NootubeController(
        IVideoService videoService,
        ICommentService commentService,
        IMapper mapper
    )
        : base(mapper)
    {
        _videoService = videoService;
        _commentService = commentService;
    }

    /// <summary>
    /// Gets a list of videos, paginated
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = NooTubePolicies.CanGetNooTubeVideos)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<NooTubeVideoDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetVideosAsync([FromQuery] VideoFilter filter)
    {
        var result = await _videoService.GetAsync(filter);

        return SendResponse<NooTubeVideoModel, NooTubeVideoDTO>(result);
    }

    /// <summary>
    /// Gets a video by its ID.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{videoId:ulid}")]
    [Authorize(Policy = NooTubePolicies.CanGetNooTubeVideos)]
    [Produces(
        typeof(ApiResponseDTO<NooTubeVideoDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetVideoByIdAsync([FromRoute] Ulid videoId)
    {
        var video = await _videoService.GetByIdAsync(videoId);

        return SendResponse<NooTubeVideoModel, NooTubeVideoDTO>(video);
    }

    /// <summary>
    /// Creates a video and initializes an upload on the configured video engine.
    /// Returns the upload URL the client should stream the file to (e.g. a tus endpoint).
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost]
    [Authorize(Policy = NooTubePolicies.CanEditNooTubeVideos)]
    [Produces(
        typeof(ApiResponseDTO<NooTubeVideoUploadDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateVideoAsync([FromBody] CreateNooTubeVideoDTO createDto)
    {
        var result = await _videoService.CreateAsync(createDto);

        return SendResponse(result);
    }

    /// <summary>
    /// Marks a video upload as finished and syncs its metadata from the video engine.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{videoId:ulid}/finish")]
    [Authorize(Policy = NooTubePolicies.CanEditNooTubeVideos)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> FinishVideoUploadAsync([FromRoute] Ulid videoId)
    {
        await _videoService.FinishUploadAsync(videoId);

        return SendResponse();
    }

    /// <summary>
    /// Toggles a reaction for a video
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{videoId:ulid}/reaction")]
    [Authorize(Policy = NooTubePolicies.CanGetNooTubeVideos)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ToggleReactionAsync(
        [FromRoute] Ulid videoId,
        [FromQuery] VideoReaction reaction
    )
    {
        await _videoService.ToggleReactionAsync(videoId, reaction);

        return SendResponse();
    }

    /// <summary>
    /// Toggles whether a video is in the current user's favourites.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{videoId:ulid}/favourite")]
    [Authorize(Policy = NooTubePolicies.CanGetNooTubeVideos)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ToggleFavouriteAsync([FromRoute] Ulid videoId)
    {
        await _videoService.ToggleFavouriteAsync(videoId);

        return SendResponse();
    }

    /// <summary>
    /// Updates a video.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{videoId:ulid}")]
    [Authorize(Policy = NooTubePolicies.CanEditNooTubeVideos)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateVideoAsync(
        [FromRoute] Ulid videoId,
        [FromBody] JsonPatchDocument<UpdateNooTubeVideoDTO> patch
    )
    {
        await _videoService.UpdateAsync(videoId, patch);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a video.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{videoId:ulid}")]
    [Authorize(Policy = NooTubePolicies.CanDeleteNooTubeVideos)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeleteVideoAsync([FromRoute] Ulid videoId)
    {
        await _videoService.DeleteAsync(videoId);

        return SendResponse();
    }

    /// <summary>
    /// Gets a paginated list of comments for a video.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{videoId:ulid}/comment")]
    [Authorize(Policy = NooTubePolicies.CanGetNooTubeVideos)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<NooTubeVideoCommentDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetCommentsAsync(
        [FromRoute] Ulid videoId,
        [FromQuery] CommentFilter filter
    )
    {
        filter.VideoId = videoId;
        var result = await _commentService.GetAsync(filter);

        return SendResponse<NooTubeVideoCommentModel, NooTubeVideoCommentDTO>(result);
    }

    /// <summary>
    /// Creates a comment for a video.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{videoId:ulid}/comment")]
    [Authorize(Policy = NooTubePolicies.CanCommentOnNooTubeVideos)]
    [Produces(
        null,
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public IActionResult CreateComment(
        [FromRoute] Ulid videoId,
        [FromBody] CreateNooTubeVideoCommentDTO createDto
    )
    {
        _commentService.CreateComment(videoId, createDto);

        return SendResponse();
    }

    /// <summary>
    /// Updates a comment.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{videoId:ulid}/comment/{commentId:ulid}")]
    [Authorize(Policy = NooTubePolicies.CanEditNooTubeComments)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateCommentAsync(
        [FromRoute] Ulid commentId,
        [FromBody] JsonPatchDocument<UpdateNooTubeVideoCommentDTO> patch
    )
    {
        await _commentService.UpdateAsync(commentId, patch);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a comment.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{videoId:ulid}/comment/{commentId:ulid}")]
    [Authorize(Policy = NooTubePolicies.CanDeleteNooTubeComments)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeleteCommentAsync([FromRoute] Ulid commentId)
    {
        await _commentService.DeleteCommentAsync(commentId);

        return SendResponse();
    }
}
