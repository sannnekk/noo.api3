using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Notifications.DTO;

public record PushSubscriptionDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "PushSubscription";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}
