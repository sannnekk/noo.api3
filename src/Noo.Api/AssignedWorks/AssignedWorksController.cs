using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Services;
using Noo.Api.Core.DataAbstraction.Criteria;
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

    public AssignedWorkController(IAssignedWorkService assignedWorkService)
    {
        _assignedWorkService = assignedWorkService;
    }

    /// <summary>
    /// Gets the current user's list of assigned works.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("of-user/{userId?}")]
    [Authorize(Policy = AssignedWorkPolicies.CanGetAssignedWorks)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<AssignedWorkDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetAssignedWorksAsync(
        [FromQuery] Criteria<AssignedWorkModel> criteria,
        [FromRoute] Ulid? userId = null
    )
    {
        var neededUserId = userId is null ? User.GetId() : (Ulid)userId;
        var userRole = User.GetRole();

        if (userRole == UserRoles.Student)
        {
            var result = await _assignedWorkService.GetStudentAssignedWorksAsync(neededUserId, criteria);

            return OkResponse(result);
        }
        else if (userRole == UserRoles.Mentor)
        {
            var result = await _assignedWorkService.GetMentorAssignedWorksAsync(neededUserId, criteria);

            return OkResponse(result);
        }

        return Forbid();
    }

    /// <summary>
    /// Gets an assigned work by its ID.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{assignedWorkId}")]
    [Authorize(Policy = AssignedWorkPolicies.CanGetAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<AssignedWorkDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        var result = await _assignedWorkService.GetAssignedWorkAsync(assignedWorkId, userId, userRole);

        return result is null ? NotFound() : OkResponse(result);
    }

    /// <summary>
    /// Gets the progress of an assigned work by its ID.
    /// Used when loading the whole assigned work is not needed
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("{assignedWorkId}/progress")]
    [Authorize(Policy = AssignedWorkPolicies.CanGetAssignedWorkProgress)]
    [Produces(
        typeof(ApiResponseDTO<AssignedWorkProgressDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetAssignedWorkProgressAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        var result = await _assignedWorkService.GetAssignedWorkProgressAsync(assignedWorkId, userId, userRole);

        return result is null ? NotFound() : OkResponse(result);
    }

    /// <summary>
    /// Remakes an assigned work.
    /// Returns the ID of the newly created assigned work.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/remake")]
    [Authorize(Policy = AssignedWorkPolicies.CanRemakeAssignedWork)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status200OK,
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
        var userId = User.GetId();
        var userRole = User.GetRole();

        var id = await _assignedWorkService.RemakeAssignedWorkAsync(assignedWorkId, userId, userRole, options);

        return CreatedResponse(id);
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
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status200OK,
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
        var userId = User.GetId();
        var userRole = User.GetRole();

        var id = await _assignedWorkService.SaveAnswerAsync(assignedWorkId, userId, userRole, answer);

        return OkResponse(id);
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
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> CommentAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId,
        [FromBody] UpsertAssignedWorkCommentDTO comment
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        var id = await _assignedWorkService.SaveCommentAsync(assignedWorkId, userId, userRole, comment);

        return OkResponse(id);
    }

    /// <summary>
    /// Marks an assigned work as solved.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/mark-solved")]
    [Authorize(Policy = AssignedWorkPolicies.CanSolveAssignedWork)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> MarkAssignedWorkAsSolvedAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        await _assignedWorkService.MarkAssignedWorkAsSolvedAsync(assignedWorkId, userId);

        return NoContent();
    }

    /// <summary>
    /// Marks an assigned work as checked
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{assignedWorkId}/mark-checked")]
    [Authorize(Policy = AssignedWorkPolicies.CanCheckAssignedWork)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> MarkAssignedWorkAsCheckedAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        await _assignedWorkService.MarkAssignedWorkAsCheckedAsync(assignedWorkId, userId);

        return NoContent();
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
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ArchiveAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        await _assignedWorkService.ArchiveAssignedWorkAsync(assignedWorkId, userId, userRole);

        return NoContent();
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
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UnarchiveAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        await _assignedWorkService.UnarchiveAssignedWorkAsync(assignedWorkId, userId, userRole);

        return NoContent();
    }

    /// <summary>
    /// Adds a helper mentor to the assigned work so that they both can check the assigned work.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/add-helper-mentor")]
    [Authorize(Policy = AssignedWorkPolicies.CanAddHelperMentorToAssignedWork)]
    [Produces(
        null, StatusCodes.Status204NoContent,
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
        var userId = User.GetId();

        await _assignedWorkService.AddHelperMentorToAssignedWorkAsync(assignedWorkId, userId, options);

        return NoContent();
    }

    /// <summary>
    /// Replace main mentor of the assigned work
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/replace-main-mentor")]
    [Authorize(Policy = AssignedWorkPolicies.CanReplaceMainMentorOfAssignedWork)]
    [Produces(
        null, StatusCodes.Status204NoContent,
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
        await _assignedWorkService.ReplaceMainMentorOfAssignedWorkAsync(assignedWorkId, options);

        return NoContent();
    }

    /// <summary>
    /// Shift deadline of the assigned work
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/shift-deadline")]
    [Authorize(Policy = AssignedWorkPolicies.CanShiftDeadlineOfAssignedWork)]
    [Produces(
        null, StatusCodes.Status204NoContent,
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
        var userId = User.GetId();
        var userRole = User.GetRole();

        await _assignedWorkService.ShiftDeadlineOfAssignedWorkAsync(assignedWorkId, userId, userRole, options);

        return NoContent();
    }

    /// <summary>
    /// Changes the solve status of the assigned work back to in progress.
    /// This is usually used when the assigned work was marked as solved by mistake.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/return-to-solve")]
    [Authorize(Policy = AssignedWorkPolicies.CanReturnAssignedWorkToSolve)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ReturnAssignedWorkToSolveAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        await _assignedWorkService.ReturnAssignedWorkToSolveAsync(assignedWorkId, userId, userRole);

        return NoContent();
    }

    /// <summary>
    /// Changes the check status of the assigned work back to in progress.
    /// This is usually used when the assigned work was marked as checked by mistake.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{assignedWorkId}/return-to-check")]
    [Authorize(Policy = AssignedWorkPolicies.CanReturnAssignedWorkToCheck)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ReturnAssignedWorkToCheckAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        await _assignedWorkService.ReturnAssignedWorkToCheckAsync(assignedWorkId, userId, userRole);

        return NoContent();
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
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> DeleteAssignedWorkAsync(
        [FromRoute] Ulid assignedWorkId
    )
    {
        var userId = User.GetId();
        var userRole = User.GetRole();

        await _assignedWorkService.DeleteAssignedWorkAsync(assignedWorkId, userId, userRole);

        return NoContent();
    }
}
