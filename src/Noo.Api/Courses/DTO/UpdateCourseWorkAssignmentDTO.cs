using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.AutoMapper;

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

    [MoscowEndOfDay]
    [JsonPropertyName("solveDeadlineAt")]
    public DateTime? SolveDeadlineAt { get; init; }

    [MoscowEndOfDay]
    [JsonPropertyName("checkDeadlineAt")]
    public DateTime? CheckDeadlineAt { get; init; }
}
