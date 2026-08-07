using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.SavedTasks.DTO;

/// <summary>
/// A saved task stripped down to what a work page needs to tell saved tasks from
/// unsaved ones and to unsave them again — without pulling the tasks themselves,
/// content and all, a second time.
/// </summary>
public record SavedTaskReferenceDTO
{
    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }
}
