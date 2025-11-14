using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Users.DTO;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Users;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("/user")]
public class UserController : ApiController
{
    private readonly IUserService _userService;

    private readonly IMentorService _mentorService;

    public UserController(
        IUserService userService,
        IMentorService mentorService,
        IMapper mapper
    ) : base(mapper)
    {
        _userService = userService;
        _mentorService = mentorService;
    }

    /// <summary>
    /// Retrieves a list of users based on the provided search criteria.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = UserPolicies.CanSearchUsers)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<UserDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetUsersAsync([FromQuery] UserFilter filter)
    {
        var result = await _userService.GetUsersAsync(filter);

        return SendResponse<UserModel, UserDTO>(result);
    }

    /// <summary>
    /// Retrieves a user by their unique username
    /// </summary>
    [HttpGet("{userId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanGetUser)]
    [Produces(
        typeof(ApiResponseDTO<UserDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] Ulid userId)
    {
        var result = await _userService.GetUserByIdAsync(userId);

        return SendResponse<UserModel, UserDTO>(result);
    }

    /// <summary>
    /// Patches a user by their unique identifier.
    /// </summary>
    [HttpPatch("{userId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanPatchUser)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> PatchUserAsync([FromRoute] Ulid userId, [FromBody] JsonPatchDocument<UpdateUserDTO> patchUser)
    {
        await _userService.UpdateUserAsync(userId, patchUser);

        return SendResponse();
    }


    /// <summary>
    /// Changes the role of a user by their unique identifier.
    /// Only possible to change role if the user is a student, otherwise it will throw a conflict error.
    /// </summary>
    /// <remarks>
    /// After role change, all the user sessions will be invalidated, the user will be logged out.
    /// </remarks>
    [HttpPatch("{userId}/role")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanChangeRole)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> ChangeRoleAsync([FromRoute] Ulid userId, [FromBody] UserRoles newRole)
    {
        await _userService.ChangeRoleAsync(userId, newRole);

        return SendResponse();
    }

    /// <summary>
    /// Blocks a user by their unique identifier.
    /// </summary>
    [HttpPatch("{userId}/block")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanBlockUser)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> BlockUserAsync([FromRoute] Ulid userId)
    {
        await _userService.BlockUserAsync(userId);

        return SendResponse();
    }

    /// <summary>
    /// Unblocks a user by their unique identifier.
    /// </summary>
    [HttpPatch("{userId}/unblock")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanBlockUser)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UnblockUserAsync([FromRoute] Ulid userId)
    {
        await _userService.UnblockUserAsync(userId);

        return SendResponse();
    }

    /// <summary>
    /// Verifies a user manually by their unique identifier.
    /// </summary>
    [HttpPatch("{userId}/verify-manual")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanVerifyUser)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> VerifyUserAsync([FromRoute] Ulid userId)
    {
        await _userService.VerifyUserAsync(userId);

        return SendResponse();
    }

    /// <summary>
    /// Retrieves a student's mentor assignments by their unique identifier.
    /// </summary>
    [HttpGet("{studentId}/mentor-assignment")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanGetUser)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<MentorAssignmentDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetMentorAssignmentsAsync([FromRoute] Ulid studentId, [FromQuery] MentorAssignmentFilter filter)
    {
        var result = await _mentorService.GetMentorAssignmentsAsync(studentId, filter);

        return SendResponse<MentorAssignmentModel, MentorAssignmentDTO>(result);
    }

    /// <summary>
    /// Retrieves a mentor's assignments by their unique identifier.
    /// </summary>
    [HttpGet("{mentorId}/student-assignment")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanGetUser)]
    [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<MentorAssignmentDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SerializedNooException), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SerializedNooException), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(SerializedNooException), StatusCodes.Status403Forbidden)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<MentorAssignmentDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetStudentAssignmentsAsync([FromRoute] Ulid mentorId, [FromQuery] MentorAssignmentFilter filter)
    {
        var result = await _mentorService.GetStudentAssignmentsAsync(mentorId, filter);

        return SendResponse<MentorAssignmentModel, MentorAssignmentDTO>(result);
    }

    /// <summary>
    /// Assigns a mentor to a student for a specific subject.
    /// </summary>
    /// <remarks>
    /// If a user already has a mentor assigned for the subject, it will be unassigned and a new one will be assigned.
    /// </remarks>
    [HttpPatch("{studentId}/assign-mentor")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanAssignMentor)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict
    )]
    public async Task<IActionResult> AssignMentorAsync([FromRoute] Ulid studentId, [FromBody] CreateMentorAssignmentDTO assignment)
    {
        var assignmentId = await _mentorService.AssignMentorAsync(studentId, assignment.MentorId, assignment.SubjectId);

        return SendResponse(assignmentId);
    }

    /// <summary>
    /// Unassigns a mentor from a student.
    /// </summary>
    [HttpPatch("{studentId}/unassign-mentor")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanAssignMentor)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UnassignMentorAsync([FromRoute] Ulid studentId)
    {
        await _mentorService.UnassignMentorAsync(studentId);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a user by their unique identifier.
    /// </summary>
    /// <remarks>
    /// The user is only soft-deleted, meaning their data is retained but they can no longer log in and do not appear in user searches.
    /// </remarks>
    /// TODO: implement soft deleting
    [HttpDelete("{userId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = UserPolicies.CanDeleteUser)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Ulid userId)
    {
        await _userService.DeleteUserAsync(userId);
        return SendResponse();
    }
}
