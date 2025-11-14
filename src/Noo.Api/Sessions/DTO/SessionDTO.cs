using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.UserAgent;

namespace Noo.Api.Sessions.DTO;

public record SessionDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "Session";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("lastRequestAt")]
    public DateTime LastRequestAt { get; init; }

    [JsonPropertyName("device")]
    public string? Device { get; init; }

    [JsonPropertyName("os")]
    public string? Os { get; init; }

    [JsonPropertyName("browser")]
    public string? Browser { get; init; }

    [Required]
    [JsonPropertyName("deviceType")]
    public DeviceType DeviceType { get; init; } = DeviceType.Unknown;
}
