using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.SavedTasks.DTO;
using Noo.Api.SavedTasks.Filters;
using Noo.Api.SavedTasks.Models;
using Noo.Api.SavedTasks.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.SavedTasks;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("saved-task")]
public class SavedTaskController : ApiController
{
    private readonly ISavedTaskService _savedTaskService;

    public SavedTaskController(ISavedTaskService savedTaskService, IMapper mapper)
        : base(mapper)
    {
        _savedTaskService = savedTaskService;
    }

    /// <summary>
    /// Get a paginated list of the authenticated student's saved tasks.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = SavedTaskPolicies.CanGetSavedTasks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<SavedTaskDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetSavedTasksAsync([FromQuery] SavedTaskFilter filter)
    {
        var result = await _savedTaskService.GetSavedTasksAsync(filter);

        return SendResponse<SavedTaskModel, SavedTaskDTO>(result);
    }

    /// <summary>
    /// Get the authenticated student's saved tasks as bare references,
    /// optionally only the ones saved from one assigned work.
    /// </summary>
    /// <remarks>
    /// Lets a work page tell saved tasks from unsaved ones without pulling the
    /// saved tasks themselves, content and all.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("reference")]
    [Authorize(Policy = SavedTaskPolicies.CanGetSavedTasks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<SavedTaskReferenceDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetSavedTaskReferencesAsync([FromQuery] Ulid? assignedWorkId)
    {
        var result = await _savedTaskService.GetReferencesAsync(assignedWorkId);

        return SendResponse(result);
    }

    /// <summary>
    /// Get the subjects the authenticated student has saved tasks on, with how
    /// many on each. What a quiz is set up from.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("subject")]
    [Authorize(Policy = SavedTaskPolicies.CanGetSavedTasks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<SavedTaskSubjectDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetSavedTaskSubjectsAsync()
    {
        var result = await _savedTaskService.GetSubjectSummariesAsync();

        return SendResponse(result);
    }

    /// <summary>
    /// Get a random deck of the authenticated student's saved tasks to run a
    /// quiz on, optionally drawn from one subject only.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("quiz")]
    [Authorize(Policy = SavedTaskPolicies.CanGetSavedTasks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<SavedTaskDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> GetQuizDeckAsync(
        [FromQuery] Ulid? subjectId,
        [FromQuery] [Range(SavedTaskConfig.MinQuizCardCount, SavedTaskConfig.MaxQuizCardCount)]
            int count = SavedTaskConfig.MinQuizCardCount
    )
    {
        var result = await _savedTaskService.GetQuizDeckAsync(subjectId, count);

        return SendResponse<IEnumerable<SavedTaskDTO>>(
            _mapper.Map<IEnumerable<SavedTaskDTO>>(result)
        );
    }

    /// <summary>
    /// Check an answer to one saved task against its answer key.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{savedTaskId}/check")]
    [Authorize(Policy = SavedTaskPolicies.CanGetSavedTasks)]
    [Produces(
        typeof(ApiResponseDTO<SavedTaskAnswerCheckDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> CheckSavedTaskAnswerAsync(
        [FromRoute] Ulid savedTaskId,
        [FromBody] CheckSavedTaskAnswerDTO checkAnswerDto
    )
    {
        var result = await _savedTaskService.CheckAnswerAsync(savedTaskId, checkAnswerDto);

        return SendResponse(result);
    }

    /// <summary>
    /// Save a task of a checked assigned work of the authenticated student.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost]
    [Authorize(Policy = SavedTaskPolicies.CanSaveTask)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> SaveTaskAsync([FromBody] CreateSavedTaskDTO createSavedTaskDto)
    {
        var savedTaskId = await _savedTaskService.CreateSavedTaskAsync(createSavedTaskDto);

        return SendResponse(savedTaskId);
    }

    /// <summary>
    /// Remove a saved task.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{savedTaskId}")]
    [Authorize(Policy = SavedTaskPolicies.CanRemoveSavedTask)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> RemoveSavedTaskAsync([FromRoute] Ulid savedTaskId)
    {
        await _savedTaskService.DeleteSavedTaskAsync(savedTaskId);

        return SendResponse();
    }
}
