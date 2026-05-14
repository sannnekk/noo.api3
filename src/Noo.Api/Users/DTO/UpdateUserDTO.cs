using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Users.DTO;

public record UpdateUserDTO
{
    [JsonPropertyName("username")]
    [MinLength(1)]
    [MaxLength(63)]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    [EmailAddress]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    [MinLength(1)]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [JsonPropertyName("name")]
    [MinLength(1)]
    [MaxLength(255)]
    public string? Name { get; set; }

    [JsonPropertyName("telegramId")]
    [MinLength(1)]
    [MaxLength(63)]
    public string? TelegramId { get; set; }

    [JsonPropertyName("telegramUsername")]
    [MinLength(1)]
    [MaxLength(255)]
    public string? TelegramUsername { get; set; }
}
