using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noo.Api.Core.Request;
using Noo.Api.Core.Response;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.Versioning;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Models;
using Noo.Api.Notifications.WebPush;
using ProducesAttribute = Noo.Api.Core.Documentation.ProducesAttribute;

namespace Noo.Api.Notifications;

[ApiVersion(NooApiVersions.Current)]
[ApiController]
[Route("push-subscription")]
public class PushSubscriptionController : ApiController
{
    private readonly IPushSubscriptionService _pushSubscriptionService;

    public PushSubscriptionController(IPushSubscriptionService pushSubscriptionService, IMapper mapper)
        : base(mapper)
    {
        _pushSubscriptionService = pushSubscriptionService;
    }

    /// <summary>
    /// Gets the VAPID public key browsers subscribe with, and whether push is enabled on the server.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet("vapid-public-key")]
    [Authorize(Policy = NotificationPolicies.CanManagePushSubscriptions)]
    [Produces(
        typeof(ApiResponseDTO<WebPushPublicKeyDTO>), StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public IActionResult GetPublicKey()
    {
        return SendResponse(_pushSubscriptionService.GetPublicKey());
    }

    /// <summary>
    /// Lists the devices the current user receives push notifications on.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpGet]
    [Authorize(Policy = NotificationPolicies.CanManagePushSubscriptions)]
    [Produces(
        typeof(ApiResponseDTO<IEnumerable<PushSubscriptionDTO>>), StatusCodes.Status200OK,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> GetSubscriptionsAsync()
    {
        var subscriptions = await _pushSubscriptionService.GetForUserAsync(User.GetId());

        return SendResponse<IEnumerable<PushSubscriptionModel>, IEnumerable<PushSubscriptionDTO>>(subscriptions);
    }

    /// <summary>
    /// Subscribes the calling browser to push notifications.
    /// </summary>
    /// <remarks>
    /// Idempotent per endpoint: subscribing an already known browser updates its keys and moves it to the current user.
    /// </remarks>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost]
    [Authorize(Policy = NotificationPolicies.CanManagePushSubscriptions)]
    [Produces(
        typeof(ApiResponseDTO<IdResponseDTO>), StatusCodes.Status201Created,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> SubscribeAsync([FromBody] CreatePushSubscriptionDTO dto)
    {
        var id = await _pushSubscriptionService.SubscribeAsync(User.GetId(), dto);

        return SendResponse(id);
    }

    /// <summary>
    /// Unsubscribes the browser with the given push endpoint.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete]
    [Authorize(Policy = NotificationPolicies.CanManagePushSubscriptions)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> UnsubscribeAsync([FromQuery] string endpoint)
    {
        await _pushSubscriptionService.UnsubscribeAsync(User.GetId(), endpoint);

        return SendResponse();
    }

    /// <summary>
    /// Removes one of the current user's devices.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpDelete("{subscriptionId}")]
    [Authorize(Policy = NotificationPolicies.CanManagePushSubscriptions)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> DeleteSubscriptionAsync([FromRoute] Ulid subscriptionId)
    {
        await _pushSubscriptionService.DeleteAsync(User.GetId(), subscriptionId);

        return SendResponse();
    }

    /// <summary>
    /// Sends a test push to every device of the current user. Nothing is stored.
    /// </summary>
    [MapToApiVersion(NooApiVersions.Current)]
    [HttpPost("test")]
    [Authorize(Policy = NotificationPolicies.CanManagePushSubscriptions)]
    [Produces(
        null, StatusCodes.Status204NoContent,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden
    )]
    public async Task<IActionResult> SendTestAsync(CancellationToken ct)
    {
        var payload = new WebPushPayload
        {
            Type = "push.test",
            Title = "Проверка уведомлений",
            Body = "Push-уведомления работают на этом устройстве",
        };

        await _pushSubscriptionService.SendToUserAsync(User.GetId(), payload, urgent: true, ct);

        return SendResponse();
    }
}
