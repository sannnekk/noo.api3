using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Users.DTO;

/// <summary>
/// DTO for updating Telegram user information.
/// Does not follow the same pattern as other DTOs because it is generated by Telegram.
/// </summary>
public record UpdateTelegramDTO
{
    [JsonPropertyName("username")]
    public string? TelegramUsername { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("id")]
    public string TelegramId { get; set; } = string.Empty;

    [Url]
    [JsonPropertyName("photo_url")]
    public string? TelegramAvatarUrl { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? TelegramFirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string? TelegramLastName { get; set; } = string.Empty;

    [JsonPropertyName("auth_date")]
    public long TelegramAuthTimestamp { get; set; }

    [JsonIgnore]
    public DateTime TelegramAuthDate
    {
        get => DateTimeOffset.FromUnixTimeSeconds(TelegramAuthTimestamp).DateTime;
    }

    [JsonPropertyName("hash")]
    public string TelegramHash { get; set; } = string.Empty;
}
