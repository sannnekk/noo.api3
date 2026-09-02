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
    private readonly ISupportFaqService _supportFaqService;

    public SupportController(
        ISupportService supportService,
        ISupportFaqService supportFaqService,
        IMapper mapper
    )
        : base(mapper)
    {
        _supportService = supportService;
        _supportFaqService = supportFaqService;
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

    /// <summary>
    /// Retrieves the frequently asked questions shown on the help home page.
    /// </summary>
    [HttpGet("faq")]
    [MapToApiVersion(NooApiVersions.Current)]
    [AllowAnonymous]
    [Produces(
        typeof(ApiResponseDTO<List<SupportFaqItemDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest
    )]
    public async Task<IActionResult> GetFaqItemsAsync([FromQuery] SupportFaqItemFilter filter)
    {
        var response = await _supportFaqService.GetItemsAsync(filter);

        return SendResponse<SupportFaqItemModel, SupportFaqItemDTO>(response);
    }

    /// <summary>
    /// Creates a new frequently asked question.
    /// </summary>
    [HttpPost("faq")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SupportPolicies.CanCreateFaqItem)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult CreateFaqItem([FromBody] CreateSupportFaqItemDTO request)
    {
        var id = _supportFaqService.CreateItem(request);

        return SendResponse(id);
    }

    /// <summary>
    /// Updates a frequently asked question by its ID using a JSON Patch document.
    /// </summary>
    [HttpPatch("faq/{itemId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SupportPolicies.CanUpdateFaqItem)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateFaqItemAsync(
        [FromRoute] Ulid itemId,
        [FromBody] JsonPatchDocument<UpdateSupportFaqItemDTO> request
    )
    {
        await _supportFaqService.UpdateItemAsync(itemId, request);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a frequently asked question by its ID.
    /// </summary>
    [HttpDelete("faq/{itemId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SupportPolicies.CanDeleteFaqItem)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult DeleteFaqItem([FromRoute] Ulid itemId)
    {
        _supportFaqService.DeleteItem(itemId);

        return SendResponse();
    }
}
