using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.Services;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Courses;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("course")]
public class CourseController : ApiController
{
    private readonly ICourseService _courseService;

    private readonly ICourseMembershipService _courseMembershipService;

    public CourseController(
        ICourseService courseService,
        ICourseMembershipService courseMembershipService,
        IMapper mapper
    ) : base(mapper)
    {
        _courseService = courseService;
        _courseMembershipService = courseMembershipService;
    }

    /// <summary>
    /// Retrieves a course and its chapter/material tree by its unique identifier.
    /// </summary>
    [HttpGet("{courseId:ulid}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanGetCourse)]
    [Produces(
        typeof(ApiResponseDTO<CourseDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetCourseAsync([FromRoute] Ulid courseId)
    {
        var course = await _courseService.GetByIdAsync(courseId, false);

        return SendResponse<CourseModel, CourseDTO>(course);
    }

    /// <summary>
    /// Retrieves a material content by its unique identifier.
    /// </summary>
    [HttpGet("{courseId}/content/{contentId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanGetCourse)]
    [Produces(
        typeof(ApiResponseDTO<CourseDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetCourseContentAsync([FromRoute] Ulid contentId)
    {
        var content = await _courseService.GetContentByIdAsync(contentId);

        return SendResponse<CourseMaterialContentModel, CourseMaterialContentDTO>(content);
    }

    /// <summary>
    /// Search courses
    /// </summary>
    [HttpGet]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanSearchCourses)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<CourseDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetCoursesAsync([FromQuery] CourseFilter filter)
    {
        var result = await _courseService.SearchAsync(filter);

        return SendResponse<CourseModel, CourseDTO>(result);
    }

    /// <summary>
    /// Creates a course.
    /// </summary>
    [HttpPost]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanCreateCourse)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateCourseAsync([FromBody] CreateCourseDTO dto)
    {
        var id = await _courseService.CreateAsync(dto);

        return SendResponse(id);
    }

    /// <summary>
    /// Updates a course.
    /// </summary>
    [HttpPatch("{courseId:ulid}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanEditCourse)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateCourseAsync([FromRoute] Ulid courseId,
        [FromBody] JsonPatchDocument<UpdateCourseDTO> courseUpdateDto)
    {
        await _courseService.UpdateAsync(courseId, courseUpdateDto);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a course.
    /// </summary>
    [HttpDelete("{courseId:ulid}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanDeleteCourse)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeleteCourseAsync([FromRoute] Ulid courseId)
    {
        await _courseService.SoftDeleteAsync(courseId);

        return SendResponse();
    }

    /// <summary>
    /// Creates a course material content.
    /// </summary>
    [HttpPost("material-content")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanCreateCourse)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateCourseMaterialContentAsync([FromBody] CreateCourseMaterialContentDTO dto)
    {
        var id = await _courseService.CreateMaterialContentAsync(dto);

        return SendResponse(id);
    }

    /// <summary>
    /// Updates a course material content.
    /// </summary>
    [HttpPatch("material-content/{contentId:ulid}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanEditCourse)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> UpdateCourseMaterialContentAsync([FromRoute] Ulid contentId, [FromBody] JsonPatchDocument<UpdateCourseMaterialContentDTO> contentUpdateDto)
    {
        await _courseService.UpdateContentAsync(contentId, contentUpdateDto);

        return SendResponse();
    }

    /// <summary>
    /// Searches the course memberships.
    /// </summary>
    [HttpGet("membership")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanSearchCourseMemberships)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<CourseMembershipDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetCourseMembershipsAsync([FromQuery] CourseMembershipFilter filter)
    {
        var result = await _courseMembershipService.GetMembershipsAsync(filter);

        return SendResponse<CourseMembershipModel, CourseMembershipDTO>(result);
    }

    /// <summary>
    /// Creates a course membership for a user
    /// </summary>
    [HttpPost("membership")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanCreateCourseMembership)]
    [Produces(
        typeof(ApiResponseDTO<CourseMembershipDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateCourseMembershipAsync([FromBody] CreateCourseMembershipDTO dto)
    {
        var id = await _courseMembershipService.CreateMembershipAsync(dto);

        return SendResponse(id);
    }

    /// <summary>
    /// Removes a course membership for a user
    /// </summary>
    [HttpDelete("membership/{membershipId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = CoursePolicies.CanDeleteCourseMembership)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeleteCourseMembershipAsync([FromRoute] Ulid membershipId)
    {
        await _courseMembershipService.SoftDeleteMembershipAsync(membershipId);

        return SendResponse();
    }
}
