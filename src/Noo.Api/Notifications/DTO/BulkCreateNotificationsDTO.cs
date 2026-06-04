using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils;
using Noo.Api.Notifications.Types;

namespace Noo.Api.Notifications.DTO;

public record BulkCreateNotificationsDTO
{
    [Required]
    [JsonPropertyName("userIds")]
    public IEnumerable<Ulid> UserIds { get; init; } = Enumerable.Empty<Ulid>();

    [Required]
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("isBanner")]
    public bool IsBanner { get; init; }

    [JsonPropertyName("link")]
    public FrontendLink? Link { get; init; }

    [JsonPropertyName("linkText")]
    public string? LinkText { get; init; }

    [JsonPropertyName("channels")]
    public IEnumerable<NotificationChannelType>? Channels { get; init; }
}
