using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.SavedTasks.DTO;

public record CreateSavedTaskDTO
{
    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }

    [Required]
    [JsonPropertyName("assignedWorkId")]
    public Ulid AssignedWorkId { get; init; }
}
