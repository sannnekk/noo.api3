using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record CourseChapterDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "CourseChapter";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("parentChapterId")]
    public Ulid? ParentChapterId { get; init; }

    [Required]
    [JsonPropertyName("subChapters")]
    public IEnumerable<CourseChapterDTO> SubChapters { get; init; } = [];

    [Required]
    [JsonPropertyName("materials")]
    public IEnumerable<CourseMaterialDTO> Materials { get; init; } = [];

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
