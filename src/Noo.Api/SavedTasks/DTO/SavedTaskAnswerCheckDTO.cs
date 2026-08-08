using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.SavedTasks.DTO;

/// <summary>
/// The verdict on one quiz answer, scored by the same checker that scores the
/// work it came from.
/// </summary>
public record SavedTaskAnswerCheckDTO
{
    [Required]
    [JsonPropertyName("score")]
    public int Score { get; init; }

    [Required]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; init; }

    /// <summary>
    /// Whether the answer earned every point on offer. A partially scored answer
    /// is not a right one as far as the quiz is concerned.
    /// </summary>
    [Required]
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; init; }
}
