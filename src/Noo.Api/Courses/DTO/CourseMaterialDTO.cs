using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record CourseMaterialDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "CourseMaterial";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("titleColor")]
    public string? TitleColor { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [Required]
    [JsonPropertyName("publishAt")]
    public DateTime PublishAt { get; init; }

    [Required]
    [JsonPropertyName("chapterId")]
    public Ulid ChapterId { get; init; }

    [Required]
    [JsonPropertyName("contentId")]
    public Ulid ContentId { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
