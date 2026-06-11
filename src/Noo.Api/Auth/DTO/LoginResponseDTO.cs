using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.Auth.DTO;

public record LoginResponseDTO
{
    [Required]
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [Required]
    [JsonPropertyName("userId")]
    public Ulid UserId { get; set; }

    [Required]
    [JsonPropertyName("userRole")]
    public UserRoles UserRole { get; init; }
}
