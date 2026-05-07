using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Subjects.DTO;
using Noo.Api.Subjects.Filters;
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

    public SubjectController(ISubjectService subjectService, IMapper mapper)
        : base(mapper)
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
        typeof(ApiResponseDTO<IEnumerable<SubjectDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized
    )]
    public async Task<IActionResult> GetSubjectsAsync([FromQuery] SubjectFilter filter)
    {
        var result = await _subjectService.GetSubjectsAsync(filter);

        return SendResponse<SubjectModel, SubjectDTO>(result);
    }

    /// <summary>
    /// Retrieves a subject by its unique identifier.
    /// </summary>
    [HttpGet("{subjectId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanGetSubject)]
    [Produces(
        typeof(ApiResponseDTO<SubjectDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetSubjectByIdAsync([FromRoute] Ulid subjectId)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(subjectId);

        return SendResponse<SubjectModel, SubjectDTO>(subject);
    }

    /// <summary>
    /// Creates a new subject with the provided details.
    /// </summary>
    [HttpPost]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanCreateSubject)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult CreateSubject([FromBody] SubjectCreationDTO subject)
    {
        var id = _subjectService.CreateSubject(subject);

        return SendResponse(id);
    }

    /// <summary>
    /// Updates an existing subject with the provided details.
    /// </summary>
    [HttpPatch("{subjectId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanPatchSubject)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateSubjectAsync(
        [FromRoute] Ulid subjectId,
        [FromBody] JsonPatchDocument<SubjectUpdateDTO> subject
    )
    {
        await _subjectService.UpdateSubjectAsync(subjectId, subject);

        return SendResponse();
    }

    /// <summary>
    /// Deletes a subject by its unique identifier.
    /// </summary>
    [HttpDelete("{subjectId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = SubjectPolicies.CanDeleteSubject)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public IActionResult DeleteSubject([FromRoute] Ulid subjectId)
    {
        _subjectService.DeleteSubject(subjectId);

        return SendResponse();
    }
}
