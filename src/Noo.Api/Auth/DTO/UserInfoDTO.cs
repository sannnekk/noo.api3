using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Auth.DTO;

public record UserInfoDTO
{
    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("role")]
    public UserRoles Role { get; set; } = UserRoles.Student;
}
