using System.Net;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.WebPush;

[RegisterSingleton(typeof(IWebPushSender))]
public sealed class WebPushSender : IWebPushSender, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebPushConfig _config;
    private readonly ILogger<WebPushSender> _logger;
    private readonly Lazy<VapidAuthentication> _vapid;

    public WebPushSender(
        IHttpClientFactory httpClientFactory,
        IOptions<WebPushConfig> config,
        ILogger<WebPushSender> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
        _logger = logger;
        _vapid = new Lazy<VapidAuthentication>(() =>
            new VapidAuthentication(_config.PublicKey, _config.PrivateKey) { Subject = _config.Subject }
        );
    }

    public async Task<WebPushSendResult> SendAsync(
        PushSubscriptionModel subscription,
        string payload,
        bool urgent,
        CancellationToken ct = default
    )
    {
        var target = new PushSubscription { Endpoint = subscription.Endpoint };
        target.SetKey(PushEncryptionKeyName.P256DH, subscription.P256dh);
        target.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);

        var message = new PushMessage(payload)
        {
            TimeToLive = _config.TtlSeconds,
            Urgency = urgent ? PushMessageUrgency.High : PushMessageUrgency.Normal,
        };

        // The unnamed client: the shared external one's circuit breaker would let one dead push service trip calls to Google.
        var client = new PushServiceClient(_httpClientFactory.CreateClient())
        {
            DefaultAuthentication = _vapid.Value,
        };

        try
        {
            await client.RequestPushMessageDeliveryAsync(target, message, ct);
            return WebPushSendResult.Sent;
        }
        catch (PushServiceClientException ex)
            when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return WebPushSendResult.Gone;
        }
        catch (Exception ex) when (ex is PushServiceClientException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Web push delivery to subscription {SubscriptionId} failed", subscription.Id);
            return WebPushSendResult.Failed;
        }
    }

    public void Dispose()
    {
        if (_vapid.IsValueCreated)
        {
            _vapid.Value.Dispose();
        }
    }
}
