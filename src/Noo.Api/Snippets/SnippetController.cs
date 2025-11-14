using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Snippets.DTO;
using Noo.Api.Snippets.Models;
using Noo.Api.Snippets.Services;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Snippets;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("snippet")]
public class SnippetController : ApiController
{
    private readonly ISnippetService _snippetService;

    public SnippetController(ISnippetService snippetService, IMapper mapper) : base(mapper)
    {
        _snippetService = snippetService;
    }

    /// <summary>
    /// Retrieves the snippets for the authenticated user.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = SnippetPolicies.CanGetOwnSnippets)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<SnippetDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetSnippetsAsync()
    {
        var userId = User.GetId();
        var result = await _snippetService.GetSnippetsAsync(userId);

        return SendResponse<SnippetModel, SnippetDTO>(result);
    }

    /// <summary>
    /// Creates a new snippet for the authenticated user.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost]
    [Authorize(Policy = SnippetPolicies.CanCreateSnippet)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> CreateSnippetAsync([FromBody] CreateSnippetDTO createSnippetDto)
    {
        var userId = User.GetId();
        await _snippetService.CreateSnippetAsync(userId, createSnippetDto);

        return SendResponse();
    }

    /// <summary>
    /// Updates an existing snippet for the authenticated user.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{snippetId}")]
    [Authorize(Policy = SnippetPolicies.CanEditSnippet)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateSnippetAsync(
        [FromRoute] Ulid snippetId,
        [FromBody] JsonPatchDocument<UpdateSnippetDTO> snippetUpdateDto
    )
    {
        var userId = User.GetId();
        await _snippetService.UpdateSnippetAsync(userId, snippetId, snippetUpdateDto);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a snippet for the authenticated user.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{snippetId}")]
    [Authorize(Policy = SnippetPolicies.CanDeleteSnippet)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> DeleteSnippetAsync([FromRoute] Ulid snippetId)
    {
        var userId = User.GetId();
        await _snippetService.DeleteSnippetAsync(userId, snippetId);

        return SendResponse();
    }
}
