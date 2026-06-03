using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Sessions.DTO;

public record OnlineInfoDTO
{
    [Required]
    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; init; }

    [JsonPropertyName("lastOnlineAt")]
    public DateTime? LastOnlineAt { get; init; }
}
