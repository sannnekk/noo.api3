using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Auth.DTO;

public record LoginDTO
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    [JsonPropertyName("usernameOrEmail")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(256)]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
