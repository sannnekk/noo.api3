using System.Text.Json.Serialization;

namespace Noo.Api.Core.ThirdPartyServices.Kinescope.Models;

public record KinescopeWebhook
{
    public const string VideoStatusEvent = "media.update.status";

    [JsonPropertyName("event")]
    public string? Event { get; init; }

    [JsonPropertyName("data")]
    public KinescopeWebhookData? Data { get; init; }
}

public record KinescopeWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
