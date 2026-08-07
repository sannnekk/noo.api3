using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Works.DTO;

namespace Noo.Api.SavedTasks.DTO;

public record SavedTaskDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "SavedTask";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }

    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }

    [Required]
    [JsonPropertyName("task")]
    public WorkTaskDTO Task { get; init; } = default!;

    [Required]
    [JsonPropertyName("assignedWorkId")]
    public Ulid? AssignedWorkId { get; init; }

    [Required]
    [JsonPropertyName("workId")]
    public Ulid WorkId { get; init; }

    [JsonPropertyName("work")]
    public WorkDTO? Work { get; init; }
}
