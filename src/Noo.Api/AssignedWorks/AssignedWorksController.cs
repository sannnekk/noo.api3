using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Versioning;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.AssignedWorks;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("assigned-work")]
public class AssignedWorkController : ApiController
{
    private readonly IAssignedWorkService _assignedWorkService;

    public AssignedWorkController(IAssignedWorkService assignedWorkService, IMapper mapper)
        : base(mapper)
    {
        _assignedWorkService = assignedWorkService;
    }

    /// <summary>
    /// Gets the current user's list of assigned works.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = AssignedWorkPolicies.CanGetAssignedWorks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<AssignedWorkDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetAssignedWorksAsync([FromQuery] AssignedWorkFilter filter)
    {
        var userId = User.GetId();

        switch (User.GetRole())
        {
            case UserRoles.Student:
                filter.StudentId = userId;
                break;
            case UserRoles.Mentor:
                filter.MentorId = userId;
                break;
            case UserRoles.Assistant:
            case UserRoles.Teacher:
            case UserRoles.Admin:
                break;
        }

        var result = await _assignedWorkService.GetAssignedWorksAsync(filter);

        return SendResponse<AssignedWorkModel, AssignedWorkDTO>(result);
    }

    /// <summary>
    /// Creates an assigned work instance by a work assignment ID.
    /// This is used when a student starts working on a work
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{workAssignmentId}")]
    [Authorize(Policy = AssignedWorkPolicies.CanCreateAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> CreateAssignedWorkAsync([FromRoute] Ulid workAssignmentId)
    {
        var id = await _assignedWorkService.CreateAsync(workAssignmentId);

        return SendResponse(id);
    }

    /// <summary>
    /// Gets an assigned work by its ID.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{assignedWorkId}")]
    [Authorize(Policy = AssignedWorkPolicies.CanGetAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<AssignedWorkDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetAssignedWorkAsync([FromRoute] Ulid assignedWorkId)
    {
        var result = await _assignedWorkService.GetAsync(assignedWorkId);

        return SendResponse<AssignedWorkModel, AssignedWorkDTO>(result);
    }

    /// <summary>
    /// Gets the progress of an assigned work by its ID.
    /// Used when loading the whole assigned work is not needed.
    /// </summary>
    /// <remarks>
    /// It returns an array of progress because there can be multiple AssignedWork's as separate attempts
    /// for one WorkAssignment.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{workAssignmentId}/progress")]
    [Authorize(Policy = AssignedWorkPolicies.CanGetAssignedWorkProgress)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<AssignedWorkProgressDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetAssignedWorkProgressAsync([FromRoute] Ulid workAssignmentId)
    {
        var result = await _assignedWorkService.GetByWorkAssignmentAsync(workAssignmentId);

        var progresses = result.Select(r => _mapper.Map<AssignedWorkProgressDTO>(r));

        return SendResponse(progresses);
    }

    /// <summary>
    /// Remakes an assigned work.
    /// Returns the ID of the newly created assigned work.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/remake")]
    [Authorize(Policy = AssignedWorkPolicies.CanRemakeAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> RemakeAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] RemakeAssignedWorkOptionsDTO options
    )
    {
        var id = await _assignedWorkService.RemakeAsync(assignedWorkId, options);

        return SendResponse(id);
    }

    /// <summary>
    /// Saves an Answer to an assigned work.
    /// This is used by students to submit their answers to assigned works,
    /// or by mentors to save their comments on the assigned work.
    /// If the answer already exists, it will be updated.
    /// If the answer does not exist, it will be created.
    /// </summary>
    /// <remarks>
    /// It will also update the status of the assigned work to "In Progress" if it was not already.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/save-answer")]
    [Authorize(Policy = AssignedWorkPolicies.CanEditAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> SaveAnswerAsync(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] UpsertAssignedWorkAnswerDTO answer
    )
    {
        var id = await _assignedWorkService.SaveAnswerAsync(assignedWorkId, answer);

        return SendResponse(id);
    }

    /// <summary>
    /// Saves a comment to an assigned work.
    /// This is used by both students and mentors to comment on an assigned work.
    /// If the comment already exists, it will be updated.
    /// If the comment does not exist, it will be created.
    /// </summary>
    /// <remarks>
    /// It will also update the status of the assigned work to "In Progress" if it was not already.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/comment")]
    [Authorize(Policy = AssignedWorkPolicies.CanCommentAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public IActionResult CommentAssignedWork(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] UpsertAssignedWorkCommentDTO comment
    )
    {
        var id = _assignedWorkService.SaveComment(assignedWorkId, comment);

        return SendResponse(id);
    }

    /// <summary>
    /// Marks an assigned work as solved.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/mark-solved")]
    [Authorize(Policy = AssignedWorkPolicies.CanSolveAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> MarkAssignedWorkAsSolvedAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.MarkAsSolvedAsync(assignedWorkId);

        return SendResponse();
    }

    /// <summary>
    /// Marks an assigned work as checked
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/mark-checked")]
    [Authorize(Policy = AssignedWorkPolicies.CanCheckAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> MarkAssignedWorkAsCheckedAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.MarkAsCheckedAsync(assignedWorkId);

        return SendResponse();
    }

    /// <summary>
    /// Archives an assigned work.
    /// </summary>
    /// <remarks>
    /// There is an archived flag in the assigned work for the following roles: student, mentor, and assistant.
    /// Toggling the archived flag for mentors will not affect the archived flag for students and assistants and so on.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/archive")]
    [Authorize(Policy = AssignedWorkPolicies.CanArchiveAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ArchiveAssignedWorkAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.ArchiveAsync(assignedWorkId);

        return SendResponse();
    }

    /// <summary>
    /// unarchives an assigned work.
    /// </summary>
    /// <remarks>
    /// There is an archived flag in the assigned work for the following roles: student, mentor, and assistant.
    /// Toggling the archived flag for mentors will not affect the archived flag for students and assistants and so on.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/unarchive")]
    [Authorize(Policy = AssignedWorkPolicies.CanUnarchiveAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UnarchiveAssignedWorkAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.UnarchiveAsync(assignedWorkId);

        return SendResponse();
    }

    /// <summary>
    /// Adds a helper mentor to the assigned work so that they both can check the assigned work.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/add-helper-mentor")]
    [Authorize(Policy = AssignedWorkPolicies.CanAddHelperMentorToAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> AddHelperMentorToAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] AddHelperMentorOptionsDTO options
    )
    {
        await _assignedWorkService.AddHelperMentorAsync(assignedWorkId, options);

        return SendResponse();
    }

    /// <summary>
    /// Replace main mentor of the assigned work
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/replace-main-mentor")]
    [Authorize(Policy = AssignedWorkPolicies.CanReplaceMainMentorOfAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ReplaceMainMentorOfAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] ReplaceMainMentorOptionsDTO options
    )
    {
        await _assignedWorkService.ReplaceMainMentorAsync(assignedWorkId, options);

        return SendResponse();
    }

    /// <summary>
    /// Shift deadline of the assigned work
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/shift-deadline")]
    [Authorize(Policy = AssignedWorkPolicies.CanShiftDeadlineOfAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ShiftDeadlineOfAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] ShiftAssignedWorkDeadlineOptionsDTO options
    )
    {
        await _assignedWorkService.ShiftDeadlineAsync(assignedWorkId, options);

        return SendResponse();
    }

    /// <summary>
    /// Changes the solve status of the assigned work back to in progress.
    /// This is usually used when the assigned work was marked as solved by mistake.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/return-to-solve")]
    [Authorize(Policy = AssignedWorkPolicies.CanReturnAssignedWorkToSolve)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ReturnAssignedWorkToSolveAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.ReturnToSolveAsync(assignedWorkId);

        return SendResponse();
    }

    /// <summary>
    /// Changes the check status of the assigned work back to in progress.
    /// This is usually used when the assigned work was marked as checked by mistake.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/return-to-check")]
    [Authorize(Policy = AssignedWorkPolicies.CanReturnAssignedWorkToCheck)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ReturnAssignedWorkToCheckAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.ReturnToCheckAsync(assignedWorkId);

        return SendResponse();
    }

    /// <summary>
    /// Deletes an assigned work.
    /// </summary>
    /// <remarks>
    /// Only possible if the status of the assigned work is not Solved.
    /// Otherwise, it will return a 409 Conflict status code.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{assignedWorkId}")]
    [Authorize(Policy = AssignedWorkPolicies.CanDeleteAssignedWork)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> DeleteAssignedWorkAsync([FromRoute] Ulid assignedWorkId)
    {
        await _assignedWorkService.DeleteAsync(assignedWorkId);

        return SendResponse();
    }
}
