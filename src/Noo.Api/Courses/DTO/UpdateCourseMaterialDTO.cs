using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseMaterialDTO
{
    [JsonPropertyName("id")]
    public Ulid? Id { get; init; }

    [JsonPropertyName("order")]
    public int? Order { get; init; }

    [MinLength(1)]
    [MaxLength(255)]
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [MaxLength(63)]
    [JsonPropertyName("titleColor")]
    public string? TitleColor { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("publishAt")]
    public DateTime? PublishAt { get; init; }

    [JsonPropertyName("chapterId")]
    public Ulid? ChapterId { get; init; }

    [JsonPropertyName("contentId")]
    public Ulid? ContentId { get; init; }
}
