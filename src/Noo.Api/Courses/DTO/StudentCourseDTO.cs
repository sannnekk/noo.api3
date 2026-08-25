using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Courses.Types;
using Noo.Api.Media.DTO;
using Noo.Api.Users.DTO;

namespace Noo.Api.Courses.DTO;

/// <summary>
/// One card in a student's course list: the course plus that student's own view of it. Replaces
/// <see cref="CourseMembershipDTO"/> on the student side, which could only describe courses backed
/// by a membership row.
/// </summary>
public record StudentCourseDTO : IHasPresignedMedia
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "StudentCourse";

    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(Course);
    }

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("course")]
    public CourseDTO Course { get; init; } = default!;

    [Required]
    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; init; }

    [Required]
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; init; }

    [Required]
    [JsonPropertyName("accessSource")]
    public CourseAccessSource AccessSource { get; init; }

    [JsonPropertyName("membershipType")]
    public CourseMembershipType? MembershipType { get; init; }

    [JsonPropertyName("assignedAt")]
    public DateTime? AssignedAt { get; init; }

    [JsonPropertyName("assigner")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserDTO? Assigner { get; init; }
}
