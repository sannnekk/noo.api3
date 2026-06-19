using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Config.Env;
using Noo.Api.Core.Request;
using Noo.Api.Core.ThirdPartyServices.Kinescope.Models;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.NooTube.Services;
using Noo.Api.NooTube.Types;

namespace Noo.Api.NooTube.Webhooks;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("nootube/webhooks/kinescope")]
public class KinescopeWebhookController : ControllerBase
{
    private readonly IVideoService _videoService;

    private readonly KinescopeConfig _config;

    public KinescopeWebhookController(
        IVideoService videoService,
        IOptions<KinescopeConfig> config
    )
    {
        _videoService = videoService;
        _config = config.Value;
    }

    /// <summary>
    /// Receives Kinescope webhook notifications (e.g. video processing status changes).
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> HandleAsync(
        [FromBody] KinescopeWebhook payload,
        [FromQuery(Name = "secret")] string? secret
    )
    {
        if (!IsAuthentic(secret))
        {
            return Unauthorized();
        }

        if (payload.Event == KinescopeWebhook.VideoStatusEvent && payload.Data?.Id is { } externalId)
        {
            await _videoService.UpdateEngineStatusAsync(
                NooTubeServiceType.Kinescope,
                externalId,
                payload.Data.Status
            );
        }

        return Ok();
    }

    private bool IsAuthentic(string? secret)
    {
        return string.IsNullOrEmpty(_config.WebhookSecret) || _config.WebhookSecret == secret;
    }
}
