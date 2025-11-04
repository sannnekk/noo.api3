using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Polls.DTO;

public record UpdatePollDTO
{
    [JsonPropertyName("title")]
    [MaxLength(255)]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    [MaxLength(512)]
    public string? Description { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    [JsonPropertyName("isAuthRequired")]
    public bool? IsAuthRequired { get; init; }
}
