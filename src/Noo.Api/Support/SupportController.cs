using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Filters;
using Noo.Api.Support.Models;
using Noo.Api.Support.Services;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Support;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("support")]
public class SupportController : ApiController
{
    private readonly ISupportService _supportService;

    public SupportController(ISupportService supportService, IMapper mapper)
        : base(mapper)
    {
        _supportService = supportService;
    }

    /// <summary>
    /// Retrieves a list of all support articles by category
    /// </summary>
    [HttpGet("article")]
    [MapToApiVersion(NooApiVersions.Current)]
    [AllowAnonymous]
    [Produces(
        typeof(ApiResponseDTO<List<SupportArticleDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest
    )]
    public async Task<IActionResult> GetArticlesAsync([FromQuery] SupportArticleFilter filter)
    {
        var response = await _supportService.GetArticlesAsync(filter);

        return SendResponse<SupportArticleModel, SupportArticleDTO>(response);
    }

    /// <summary>
    /// Retrieves a support article by its slug
    /// </summary>
    [HttpGet("article/{articleSlug}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [AllowAnonymous]
    [Produces(
        typeof(ApiResponseDTO<SupportArticleDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetArticleAsync([FromRoute] string articleSlug)
    {
        var response = await _supportService.GetArticleAsync(articleSlug);

        return SendResponse<SupportArticleModel, SupportArticleDTO>(response);
    }

    /// <summary>
    /// Creates a new support article.
    /// </summary>
    [HttpPost("article")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SupportPolicies.CanCreateArticle)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult CreateArticle([FromBody] CreateSupportArticleDTO request)
    {
        var id = _supportService.CreateArticle(request);

        return SendResponse(id);
    }

    /// <summary>
    /// Updates a support article by its ID using a JSON Patch document.
    /// </summary>
    [HttpPatch("article/{articleId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SupportPolicies.CanUpdateArticle)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateArticleAsync(
        [FromRoute] Ulid articleId,
        [FromBody] JsonPatchDocument<UpdateSupportArticleDTO> request
    )
    {
        await _supportService.UpdateArticleAsync(articleId, request);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a support article by its ID.
    /// </summary>
    [HttpDelete("article/{articleId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SupportPolicies.CanDeleteArticle)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult DeleteArticle([FromRoute] Ulid articleId)
    {
        _supportService.DeleteArticle(articleId);

        return SendResponse();
    }
}
