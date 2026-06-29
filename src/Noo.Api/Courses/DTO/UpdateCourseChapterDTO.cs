using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseChapterDTO
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
    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }

    [JsonPropertyName("publishAt")]
    public DateTime? PublishAt { get; init; }

    // The chapter tree is flattened for updates: every chapter (root or nested)
    // is a top-level entry in UpdateCourseDTO.Chapters keyed by its Id, and its
    // position in the tree is expressed solely through ParentChapterId. This keeps
    // the patch dictionary's semantics aligned with the EF-tracked CourseModel.Chapters
    // collection (which is the inverse of every chapter's Course FK), so the nested
    // merge can reuse tracked instances by Id without orphaning descendants.
    [JsonPropertyName("parentChapterId")]
    public Ulid? ParentChapterId { get; init; }

    [JsonPropertyName("materials")]
    public IDictionary<string, UpdateCourseMaterialDTO>? Materials { get; init; }
}
