using System.Text.Json;
using Microsoft.Extensions.Options;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Notifications.DTO;
using Noo.Api.Notifications.Models;

namespace Noo.Api.Notifications.WebPush;

[RegisterScoped(typeof(IPushSubscriptionService))]
public class PushSubscriptionService : IPushSubscriptionService
{
    private readonly IPushSubscriptionRepository _repository;
    private readonly IWebPushSender _sender;
    private readonly WebPushConfig _config;

    public PushSubscriptionService(
        IPushSubscriptionRepository repository,
        IWebPushSender sender,
        IOptions<WebPushConfig> config
    )
    {
        _repository = repository;
        _sender = sender;
        _config = config.Value;
    }

    public WebPushPublicKeyDTO GetPublicKey() => new()
    {
        Enabled = _config.IsUsable,
        PublicKey = _config.IsUsable ? _config.PublicKey : null,
    };

    public Task<List<PushSubscriptionModel>> GetForUserAsync(Ulid userId)
    {
        return _repository.GetForUserAsync(userId);
    }

    public async Task<Ulid> SubscribeAsync(Ulid userId, CreatePushSubscriptionDTO dto)
    {
        if (!Uri.TryCreate(dto.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new BadRequestException();
        }

        // A browser keeps its endpoint across logins, so the device follows whoever subscribed last.
        var existing = await _repository.GetByEndpointAsync(dto.Endpoint);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.P256dh = dto.Keys.P256dh;
            existing.Auth = dto.Keys.Auth;
            existing.UserAgent = dto.UserAgent;

            return existing.Id;
        }

        var model = new PushSubscriptionModel
        {
            UserId = userId,
            Endpoint = dto.Endpoint,
            P256dh = dto.Keys.P256dh,
            Auth = dto.Keys.Auth,
            UserAgent = dto.UserAgent,
        };

        _repository.Add(model);

        return model.Id;
    }

    public Task UnsubscribeAsync(Ulid userId, string endpoint)
    {
        return _repository.DeleteByEndpointForUserAsync(userId, endpoint);
    }

    public Task DeleteAsync(Ulid userId, Ulid subscriptionId)
    {
        return _repository.DeleteForUserAsync(userId, subscriptionId);
    }

    public async Task SendToUserAsync(
        Ulid userId,
        WebPushPayload payload,
        bool urgent,
        CancellationToken ct = default
    )
    {
        if (!_config.IsUsable)
        {
            return;
        }

        var subscriptions = await _repository.GetForUserAsync(userId, ct);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload);

        foreach (var subscription in subscriptions)
        {
            var result = await _sender.SendAsync(subscription, json, urgent, ct);

            if (result == WebPushSendResult.Gone)
            {
                _repository.Delete(subscription);
            }
        }
    }
}
