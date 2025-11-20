using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseDTO
{
    [JsonPropertyName("name")]
    [MaxLength(255)]
    public string? Name { get; init; } = string.Empty;

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("description")]
    [MaxLength(500)]
    public string? Description { get; init; }

    [JsonPropertyName("thumbnailId")]
    public Ulid? ThumbnailId { get; init; }

    [JsonPropertyName("subjectId")]
    public Ulid? SubjectId { get; init; }

    [JsonPropertyName("chapters")]
    public IDictionary<string, UpdateCourseChapterDTO>? Chapters { get; init; }
}
