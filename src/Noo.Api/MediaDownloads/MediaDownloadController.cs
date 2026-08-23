using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.MediaDownloads.DTO;
using Noo.Api.MediaDownloads.Filters;
using Noo.Api.MediaDownloads.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.MediaDownloads;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("course")]
public class MediaDownloadController : ApiController
{
    private readonly IMediaDownloadService _mediaDownloadService;

    public MediaDownloadController(IMediaDownloadService mediaDownloadService, IMapper mapper)
        : base(mapper)
    {
        _mediaDownloadService = mediaDownloadService;
    }

    /// <summary>
    /// Gets download totals for every file attached to a course material.
    ///
    /// Files nobody has downloaded are included, at zero.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("material/{materialId:ulid}/file-downloads/summary")]
    [Authorize(Policy = MediaDownloadPolicies.CanGetMaterialFileDownloads)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<MaterialFileDownloadSummaryDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetMaterialFileDownloadSummaryAsync(
        [FromRoute] Ulid materialId,
        CancellationToken cancellationToken
    )
    {
        var result = await _mediaDownloadService.GetMaterialSummaryAsync(
            materialId,
            cancellationToken
        );

        return SendResponse(result);
    }

    /// <summary>
    /// Gets who downloaded a material's files, how many times, and when they last did, paginated.
    ///
    /// The optional <c>mediaId</c> parameter narrows the breakdown to a single file.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("material/{materialId:ulid}/file-downloads")]
    [Authorize(Policy = MediaDownloadPolicies.CanGetMaterialFileDownloads)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<MaterialFileDownloaderDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetMaterialFileDownloadersAsync(
        [FromRoute] Ulid materialId,
        [FromQuery] MaterialFileDownloadsFilter filter,
        CancellationToken cancellationToken
    )
    {
        var result = await _mediaDownloadService.GetMaterialDownloadersAsync(
            materialId,
            filter,
            cancellationToken
        );

        return SendResponse(result);
    }
}
