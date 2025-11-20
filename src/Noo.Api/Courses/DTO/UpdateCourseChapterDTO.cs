using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseChapterDTO
{
    [JsonPropertyName("id")]
    public Ulid? Id { get; init; }

    [MinLength(1)]
    [MaxLength(255)]
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [MaxLength(63)]
    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("parentChapterId")]
    public Ulid? ParentChapterId { get; init; }

    [JsonPropertyName("subChapters")]
    public IDictionary<string, UpdateCourseChapterDTO>? SubChapters { get; init; }

    [JsonPropertyName("materials")]
    public IDictionary<string, UpdateCourseMaterialDTO>? Materials { get; init; }
}
