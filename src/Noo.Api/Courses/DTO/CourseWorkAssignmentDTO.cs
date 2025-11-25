using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record CourseWorkAssignmentDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "CourseWorkAssignment";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [JsonPropertyName("workId")]
    public Ulid? WorkId { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("deactivatedAt")]
    public DateTime? DeactivatedAt { get; init; }

    [JsonPropertyName("solveDeadlineAt")]
    public DateOnly? SolveDeadlineAt { get; init; }

    [JsonPropertyName("checkDeadlineAt")]
    public DateOnly? CheckDeadlineAt { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
