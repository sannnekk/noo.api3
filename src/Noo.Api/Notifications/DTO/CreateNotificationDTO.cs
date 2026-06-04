using System.Text.Json.Serialization;
using Noo.Api.Core.Utils;

namespace Noo.Api.Notifications.DTO;

public record CreateNotificationDTO
{
    [JsonPropertyName("userId")]
    public Ulid UserId { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("isBanner")]
    public bool IsBanner { get; init; }

    [JsonPropertyName("link")]
    public FrontendLink? Link { get; init; }

    [JsonPropertyName("linkText")]
    public string? LinkText { get; init; }
}
