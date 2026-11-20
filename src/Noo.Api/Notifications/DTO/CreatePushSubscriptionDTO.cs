using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Notifications.DTO;

public record CreatePushSubscriptionDTO
{
    [Required]
    [MaxLength(1024)]
    [Url]
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("keys")]
    public PushSubscriptionKeysDTO Keys { get; init; } = new();

    [MaxLength(512)]
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }
}

public record PushSubscriptionKeysDTO
{
    [Required]
    [MaxLength(127)]
    [JsonPropertyName("p256dh")]
    public string P256dh { get; init; } = string.Empty;

    [Required]
    [MaxLength(63)]
    [JsonPropertyName("auth")]
    public string Auth { get; init; } = string.Empty;
}
