using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Polls.DTO;

public record CreatePollDTO
{
    [JsonPropertyName("title")]
    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [MaxLength(512)]
    public string? Description { get; init; }

    [JsonPropertyName("isActive")]
    [Required]
    public bool? IsActive { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    [JsonPropertyName("isAuthRequired")]
    [Required]
    public bool? IsAuthRequired { get; init; }

    [JsonPropertyName("questions")]
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public IEnumerable<CreatePollQuestionDTO> Questions { get; init; } = [];
}
