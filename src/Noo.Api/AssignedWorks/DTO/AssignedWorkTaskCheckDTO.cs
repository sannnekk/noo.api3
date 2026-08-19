using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.AssignedWorks.DTO;

/// <summary>
/// The verdict on one task checked on its own, scored by the same checker that scores
/// the work as a whole.
/// </summary>
public record AssignedWorkTaskCheckDTO
{
    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }

    [Required]
    [JsonPropertyName("answerId")]
    public Ulid AnswerId { get; init; }

    [Required]
    [JsonPropertyName("score")]
    public int Score { get; init; }

    [Required]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; init; }

    /// <summary>
    /// Whether the answer earned every point on offer. A partially scored answer is not
    /// a right one.
    /// </summary>
    [Required]
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; init; }
}
