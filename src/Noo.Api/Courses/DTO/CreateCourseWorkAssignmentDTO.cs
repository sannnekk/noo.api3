using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record CreateCourseWorkAssignmentDTO
{
    [Required]
    [JsonPropertyName("order")]
    public int Order { get; init; }

    [Required]
    [JsonPropertyName("materialContentId")]
    public Ulid MaterialContentId { get; init; }

    [Required]
    [JsonPropertyName("workId")]
    public Ulid WorkId { get; init; }

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
}
