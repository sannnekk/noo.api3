using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.DataAbstraction.Criteria;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Subjects.DTO;
using Noo.Api.Subjects.Models;
using Noo.Api.Subjects.Services;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Subjects;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("subject")]
public class SubjectController : ApiController
{
    private readonly ISubjectService _subjectService;

    public SubjectController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    /// <summary>
    /// Retrieves a list of subjects based on the provided criteria.
    /// </summary>
    [HttpGet]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanGetSubjects)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<SubjectDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized
    )]
    public async Task<IActionResult> GetSubjectsAsync([FromQuery] Criteria<SubjectModel> criteria)
    {
        var (items, total) = await _subjectService.GetSubjectsAsync(criteria);
        return OkResponse((items, total));
    }

    /// <summary>
    /// Retrieves a subject by its unique identifier.
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanGetSubject)]
    [Produces(
        typeof(ApiResponseDTO<SubjectDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetSubjectByIdAsync([FromRoute] Ulid id)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id);
        return subject is null ? NotFound() : OkResponse(subject);
    }

    /// <summary>
    /// Creates a new subject with the provided details.
    /// </summary>
    [HttpPost]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanCreateSubject)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateSubjectAsync([FromBody] SubjectCreationDTO subject)
    {
        var id = await _subjectService.CreateSubjectAsync(subject);
        return CreatedResponse(id);
    }

    /// <summary>
    /// Updates an existing subject with the provided details.
    /// </summary>
    [HttpPatch("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanPatchSubject)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateSubjectAsync([FromRoute] Ulid id, [FromBody] JsonPatchDocument<SubjectUpdateDTO> subject)
    {
        await _subjectService.UpdateSubjectAsync(id, subject);

        return NoContent();
    }

    /// <summary>
    /// Deletes a subject by its unique identifier.
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanDeleteSubject)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> DeleteSubjectAsync([FromRoute] Ulid id)
    {
        await _subjectService.DeleteSubjectAsync(id);

        return NoContent();
    }
}
