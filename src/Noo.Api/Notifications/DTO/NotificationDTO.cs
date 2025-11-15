using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Notifications.DTO;

public record NotificationDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "Notification";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("isRead")]
    public bool IsRead { get; init; }

    [Required]
    [JsonPropertyName("isBanner")]
    public bool IsBanner { get; init; }

    [JsonPropertyName("link")]
    public string? Link { get; init; }

    [JsonPropertyName("linkText")]
    public string? LinkText { get; init; }
}
