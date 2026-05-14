using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Users.DTO;

public record UserDeletionDTO
{
    [Required]
    [JsonPropertyName("password")]
    public string Password { get; init; } = null!;
}
