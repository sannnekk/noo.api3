using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Media.DTO;
using Noo.Api.Media.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Media;

// Authorization is declared per-action, not on the controller: the /raw endpoint
// must also accept the cookie scheme, which a controller-level [Authorize] (default
// bearer, AND-combined with the action) would otherwise veto for cookie-only requests.
[ApiVersion(NooApiVersions.Current)]
[ApiController]
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
    [Authorize]
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
    [Authorize]
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
    [Authorize]
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
    [Authorize]
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

    /// <summary>
    /// Stable, embeddable URL for a media file. Enforces the same access rules as
    /// <see cref="GetDownloadUrlAsync"/>, then 302-redirects to a freshly presigned
    /// download URL. The redirect is never cached, so each load resolves a valid URL.
    /// </summary>
    /// <remarks>
    /// Accepts the bearer header (for API/Swagger callers) or the httpOnly media
    /// cookie, which the browser attaches to plain <c>&lt;img&gt;</c> requests.
    /// </remarks>
    [HttpGet("{mediaId:ulid}/raw")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + MediaCookie.Scheme
    )]
    [MapToApiVersion(NooApiVersions.Current)]
    [Produces(
        null,
        StatusCodes.Status302Found,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetRawAsync(
        [FromRoute] Ulid mediaId,
        CancellationToken cancellationToken
    )
    {
        var url = await _mediaService.GetDownloadUrlAsync(mediaId, cancellationToken);

        Response.Headers.CacheControl = "no-store";

        return Redirect(url);
    }
}
