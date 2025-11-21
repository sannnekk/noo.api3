using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record CreateCourseChapterDTO
{
    [JsonPropertyName("order")]
    [Required]
    public int Order { get; init; }

    [JsonPropertyName("title")]
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    [MaxLength(63)]
    public string? Color { get; set; }

    [JsonPropertyName("isActive")]
    [Required]
    public bool IsActive { get; init; }

    [JsonPropertyName("subChapters")]
    public IEnumerable<CreateCourseChapterDTO> SubChapters { get; init; } = [];

    [JsonPropertyName("materials")]
    public IEnumerable<CreateCourseMaterialDTO> Materials { get; init; } = [];
}
