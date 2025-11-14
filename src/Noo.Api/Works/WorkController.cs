using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;
using SystemTextJsonPatch;
using Noo.Api.Works.Filters;
using AutoMapper;
using Noo.Api.Works.Models;

namespace Noo.Api.Works;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("work")]
public class WorkController : ApiController
{
    private readonly IWorkService _workService;

    public WorkController(IWorkService workService, IMapper mapper) : base(mapper)
    {
        _workService = workService;
    }

    /// <summary>
    /// Searches for works based on the provided criteria.
    /// </summary>
    [HttpGet]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanSearchWorks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<WorkDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetWorksAsync([FromQuery] WorkFilter filter)
    {
        var result = await _workService.GetWorksAsync(filter);

        return SendResponse<WorkModel, WorkDTO>(result);
    }

    /// <summary>
    /// Retrieves a specific work by its ID.
    /// It will also include the work's associated tasks
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanGetWork)]
    [Produces(
        typeof(ApiResponseDTO<WorkDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetWorkAsync([FromRoute] Ulid id)
    {
        var work = await _workService.GetWorkAsync(id);

        return SendResponse<WorkModel, WorkDTO>(work);
    }

    /// <summary>
    /// Creates a new work.
    /// </summary>
    [HttpPost]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanCreateWorks)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateWorkAsync([FromBody] CreateWorkDTO work)
    {
        var id = await _workService.CreateWorkAsync(work);
        return SendResponse(id);
    }

    /// <summary>
    /// Updates an existing work.
    /// </summary>
    [HttpPatch("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanEditWorks)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateWorkAsync([FromRoute] Ulid id, [FromBody] JsonPatchDocument<UpdateWorkDTO> work)
    {
        await _workService.UpdateWorkAsync(id, work);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a work by its ID.
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = WorkPolicies.CanDeleteWorks)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> DeleteWorkAsync([FromRoute] Ulid id)
    {
        await _workService.DeleteWorkAsync(id);
        return SendResponse();
    }
}
