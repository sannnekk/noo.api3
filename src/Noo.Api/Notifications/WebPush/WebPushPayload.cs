using System.Text.Json.Serialization;
using Noo.Api.Core.Utils;

namespace Noo.Api.Notifications.WebPush;

/// <summary>
/// What the frontend service worker (public/sw.js) reads from a push.
/// </summary>
public record WebPushPayload
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("link")]
    public FrontendLink? Link { get; init; }
}
