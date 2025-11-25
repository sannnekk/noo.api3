using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseWorkAssignmentDTO
{
    [JsonPropertyName("order")]
    public int? Order { get; init; }

    [JsonPropertyName("workId")]
    public Ulid? WorkId { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("deactivatedAt")]
    public DateTime? DeactivatedAt { get; init; }

    [JsonPropertyName("solveDeadlineAt")]
    public DateOnly? SolveDeadlineAt { get; init; }

    [JsonPropertyName("checkDeadlineAt")]
    public DateOnly? CheckDeadlineAt { get; init; }
}
