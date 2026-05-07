using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    [JsonPropertyName("userInfo")]
    public UserInfoDTO UserInfo { get; set; } = new();
}
