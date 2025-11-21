using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record CreateCourseMaterialDTO
{
    [JsonPropertyName("order")]
    [Required]
    public int Order { get; init; }

    [JsonPropertyName("title")]
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("titleColor")]
    [MaxLength(63)]
    public string? TitleColor { get; set; }

    [JsonPropertyName("isActive")]
    [Required]
    public bool IsActive { get; init; }

    [JsonPropertyName("publishAt")]
    public DateTime? PublishAt { get; init; }
}
