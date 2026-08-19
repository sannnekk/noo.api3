using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.AssignedWorks.DTO;

/// <summary>
/// The answer key of one task, handed to a student who asked for it on a task that
/// offers it. Not part of the work itself: a student gets the key only by asking,
/// task by task, and only where the task allows it.
/// </summary>
public record AssignedWorkTaskAnswerKeyDTO
{
    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }

    [Required]
    [JsonPropertyName("rightAnswers")]
    public IEnumerable<string> RightAnswers { get; init; } = [];
}
