using System.Text.Json.Serialization;
using Noo.Api.Users.Types;

namespace Noo.Api.Users.DTO;

public record UpdateUserAvatarDTO
{
    [JsonPropertyName("avatarType")]
    public UserAvatarType? AvatarType { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("telegramHash")]
    public string? TelegramHash { get; init; }

    [JsonPropertyName("mediaId")]
    public Ulid? MediaId { get; init; }
}
