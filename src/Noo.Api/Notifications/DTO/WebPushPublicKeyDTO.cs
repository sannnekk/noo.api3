using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Notifications.DTO;

public record WebPushPublicKeyDTO
{
    [Required]
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; init; }
}
