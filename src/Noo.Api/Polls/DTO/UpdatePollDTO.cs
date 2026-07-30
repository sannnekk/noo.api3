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

    /// <summary>
    /// The poll questions, keyed by question Id. See <see cref="UpdatePollQuestionDTO"/>
    /// for how the keys are interpreted when the patched dictionary is merged back.
    /// </summary>
    [JsonPropertyName("questions")]
    [MaxLength(100)]
    public IDictionary<string, UpdatePollQuestionDTO>? Questions { get; init; }
}
