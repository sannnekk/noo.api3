using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Polls.DTO;
using Noo.Api.Polls.Filters;
using Noo.Api.Polls.Models;
using Noo.Api.Polls.Services;
using SystemTextJsonPatch;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Polls;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("poll")]
public class PollController : ApiController
{
    private readonly IPollService _pollService;

    public PollController(IPollService pollService, IMapper mapper) : base(mapper)
    {
        _pollService = pollService;
    }

    /// <summary>
    /// Get a list of polls based on the provided criteria.
    /// </summary>
    [HttpGet]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanGetPolls)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<PollDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetPollsAsync([FromQuery] PollFilter filter)
    {
        var result = await _pollService.GetPollsAsync(filter);

        return SendResponse<PollModel, PollDTO>(result);
    }

    /// <summary>
    /// Get a specific poll by its ID.
    /// </summary>
    [HttpGet("{pollId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanGetPoll)]
    [Produces(
        typeof(ApiResponseDTO<PollDTO>), StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetPollAsync([FromRoute] Ulid pollId)
    {
        var result = await _pollService.GetPollAsync(pollId);

        return SendResponse<PollModel, PollDTO>(result);
    }

    /// <summary>
    /// Create a new poll.
    /// </summary>
    [HttpPost]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanCreatePoll)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreatePollAsync([FromBody] CreatePollDTO poll)
    {
        var id = await _pollService.CreatePollAsync(poll);

        return SendResponse(id);
    }

    /// <summary>
    /// Update an existing poll.
    /// </summary>
    [HttpPatch("{pollId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanUpdatePoll)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdatePollAsync([FromRoute] Ulid pollId, [FromBody] JsonPatchDocument<UpdatePollDTO> poll)
    {
        await _pollService.UpdatePollAsync(pollId, poll);

        return SendResponse();
    }

    /// <summary>
    /// Delete a poll by its ID.
    /// </summary>
    [HttpDelete("{pollId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanDeletePoll)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeletePollAsync([FromRoute] Ulid pollId)
    {
        await _pollService.DeletePollAsync(pollId);

        return SendResponse();
    }

    /// <summary>
    /// Get the results of a specific poll.
    /// </summary>
    [HttpGet("{pollId}/participation")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanGetPollResults)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<PollParticipationDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetPollParticipationsAsync([FromRoute] Ulid pollId, [FromQuery] PollParticipationFilter filter)
    {
        var result = await _pollService.GetPollParticipationsAsync(pollId, filter);

        return SendResponse<PollParticipationModel, PollParticipationDTO>(result);
    }

    /// <summary>
    /// Get poll participation result by participation ID.
    /// </summary>
    [HttpGet("participation/{participationId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanGetPollResult)]
    [Produces(
        typeof(ApiResponseDTO<PollParticipationDTO>), StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> GetPollParticipationAsync([FromRoute] Ulid participationId)
    {
        var result = await _pollService.GetPollParticipationAsync(participationId);

        return SendResponse<PollParticipationModel, PollParticipationDTO>(result);
    }

    /// <summary>
    /// Create a participation in a poll.
    /// </summary>
    [HttpPost("{pollId}/participate")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanParticipateInPoll)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> ParticipateInPollAsync([FromRoute] Ulid pollId, [FromBody] CreatePollParticipationDTO participationDto)
    {
        await _pollService.ParticipateAsync(pollId, participationDto);

        return SendResponse();
    }

    /// <summary>
    /// Update a poll answer
    /// </summary>
    [HttpPatch("answer/{answerId}")]
    [MapToApiVersion(NooApiVersions.Current)]
    [Authorize(Policy = PollPolicies.CanUpdateAnswer)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdatePollAnswerAsync([FromRoute] Ulid answerId, [FromBody] JsonPatchDocument<UpdatePollAnswerDTO> updateAnswerDto)
    {
        await _pollService.UpdatePollAnswerAsync(answerId, updateAnswerDto);

        return SendResponse();
    }
}
