using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Media.DTO;
using Noo.Api.Media.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Media;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Authorize]
[Route("media")]
public class MediaController : ApiController
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService, IMapper mapper)
        : base(mapper)
    {
        _mediaService = mediaService;
    }

    /// <summary>
    /// Issue a presigned upload URL plus the headers the client must include on the PUT.
    /// </summary>
    [HttpPost("upload-url")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        typeof(ApiResponseDTO<UploadTicketDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status415UnsupportedMediaType
    )]
    public async Task<IActionResult> RequestUploadAsync(
        [FromBody] RequestUploadDTO body,
        CancellationToken cancellationToken
    )
    {
        var ticket = await _mediaService.RequestUploadAsync(
            body.Category,
            body.FileName,
            body.ContentType,
            body.EntityId,
            cancellationToken
        );

        var dto = new UploadTicketDTO
        {
            MediaId = ticket.MediaId,
            UploadUrl = ticket.Upload.Url,
            Headers = ticket.Upload.Headers,
            ExpiresAt = ticket.Upload.ExpiresAt,
        };

        return SendResponse(dto);
    }

    /// <summary>
    /// Confirm that a previously requested upload has completed in S3.
    /// Returns a presigned GET URL for the uploaded object.
    /// </summary>
    [HttpPost("{mediaId:ulid}/complete")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        typeof(ApiResponseDTO<MediaDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> CompleteUploadAsync(
        [FromRoute] Ulid mediaId,
        [FromBody] CompleteUploadDTO body,
        CancellationToken cancellationToken
    )
    {
        var media = await _mediaService.CompleteUploadAsync(
            mediaId,
            body.Size,
            body.ETag,
            cancellationToken
        );
        var dto = _mapper.Map<MediaDTO>(media);
        return SendResponse(dto);
    }

    /// <summary>
    /// Issue a short-lived presigned download URL after enforcing every access rule
    /// registered for this media's category.
    /// </summary>
    [HttpGet("{mediaId:ulid}/download-url")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        typeof(ApiResponseDTO<DownloadUrlDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetDownloadUrlAsync(
        [FromRoute] Ulid mediaId,
        CancellationToken cancellationToken
    )
    {
        var url = await _mediaService.GetDownloadUrlAsync(mediaId, cancellationToken);
        return SendResponse(new DownloadUrlDTO { Url = url });
    }

    /// <summary>
    /// Delete the file from S3 and remove the DB record. Owner or admin only.
    /// </summary>
    [HttpDelete("{mediaId:ulid}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Ulid mediaId,
        CancellationToken cancellationToken
    )
    {
        await _mediaService.DeleteAsync(mediaId, cancellationToken);
        return SendResponse();
    }
}
