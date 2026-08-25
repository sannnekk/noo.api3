using System.Text.Json.Serialization;

namespace Noo.Api.Courses.DTO;

/// <summary>
/// The current student's view of a course. Patched, so an operation the document leaves out
/// keeps whatever the student already had.
/// </summary>
public record UpdateCourseStudentStateDTO
{
    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; init; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; init; }
}
