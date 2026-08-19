using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.GoogleSheetsIntegrations.DTO;
using Noo.Api.GoogleSheetsIntegrations.Filters;
using Noo.Api.GoogleSheetsIntegrations.Models;
using Noo.Api.GoogleSheetsIntegrations.Services;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.GoogleSheetsIntegrations;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("google-sheets")]
public class GoogleSheetsIntegrationController : ApiController
{
    private readonly IGoogleSheetsIntegrationService _googleSheetsIntegrationService;

    public GoogleSheetsIntegrationController(
        IGoogleSheetsIntegrationService googleSheetsIntegrationService,
        IMapper mapper
    )
        : base(mapper)
    {
        _googleSheetsIntegrationService = googleSheetsIntegrationService;
    }

    /// <summary>
    /// Builds the Google consent URL to open before creating an integration.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("oauth-url")]
    [Authorize(Policy = GoogleSheetsIntegrationPolicies.CanCreateGoogleSheetsIntegration)]
    [Produces(
        typeof(ApiResponseDTO<GoogleOAuthUrlDTO>),
        StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult GetOAuthUrl()
    {
        return SendResponse(_googleSheetsIntegrationService.CreateOAuthUrl());
    }

    /// <summary>
    /// Retrieves the caller's own Google Sheets integrations. Nobody sees anyone else's.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = GoogleSheetsIntegrationPolicies.CanGetGoogleSheetsIntegrations)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<GoogleSheetsIntegrationDTO>>),
        StatusCodes.Status200OK,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetIntegrationsAsync(
        [FromQuery] GoogleSheetsIntegrationFilter filter
    )
    {
        var result = await _googleSheetsIntegrationService.GetIntegrationsAsync(filter);

        return SendResponse<GoogleSheetsIntegrationModel, GoogleSheetsIntegrationDTO>(result);
    }

    /// <summary>
    /// Creates a new Google Sheets integration from a freshly granted Google consent.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost]
    [Authorize(Policy = GoogleSheetsIntegrationPolicies.CanCreateGoogleSheetsIntegration)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>),
        StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> CreateIntegrationAsync(
        [FromBody] CreateGoogleSheetsIntegrationDTO request,
        CancellationToken ct
    )
    {
        var integrationId = await _googleSheetsIntegrationService.CreateIntegrationAsync(
            request,
            ct
        );

        return SendResponse(integrationId);
    }

    /// <summary>
    /// Changes an integration's name, schedule, or enabled state.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPatch("{integrationId}")]
    [Authorize(Policy = GoogleSheetsIntegrationPolicies.CanUpdateGoogleSheetsIntegration)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> UpdateIntegrationAsync(
        [FromRoute] Ulid integrationId,
        [FromBody] UpdateGoogleSheetsIntegrationDTO request
    )
    {
        await _googleSheetsIntegrationService.UpdateIntegrationAsync(integrationId, request);

        return SendResponse();
    }

    /// <summary>
    /// Queues an integration to run. The export itself happens in the background, so this
    /// returns as soon as the run is scheduled rather than when the sheet is written.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("{integrationId}/run")]
    [Authorize(Policy = GoogleSheetsIntegrationPolicies.CanRunGoogleSheetsIntegration)]
    [Produces(
        null,
        StatusCodes.Status202Accepted,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> RunIntegrationAsync(
        [FromRoute] Ulid integrationId,
        CancellationToken ct
    )
    {
        await _googleSheetsIntegrationService.QueueIntegrationAsync(integrationId, ct);

        return Accepted();
    }

    /// <summary>
    /// Deletes a Google Sheets integration. The spreadsheet itself is left untouched.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{integrationId}")]
    [Authorize(Policy = GoogleSheetsIntegrationPolicies.CanDeleteGoogleSheetsIntegration)]
    [Produces(
        null,
        StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> DeleteIntegrationAsync([FromRoute] Ulid integrationId)
    {
        await _googleSheetsIntegrationService.DeleteIntegrationAsync(integrationId);

        return SendResponse();
    }
}
