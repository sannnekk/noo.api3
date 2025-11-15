using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Courses.Types;
using Noo.Api.Users.DTO;

namespace Noo.Api.Courses.DTO;

public record CourseMembershipDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "CourseMembership";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public CourseMembershipType Type { get; init; }

    [Required]
    [JsonPropertyName("courseId")]
    public Ulid CourseId { get; init; }

    [JsonPropertyName("course")]
    public CourseDTO? Course { get; init; }

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [Required]
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; init; }

    [Required]
    [JsonPropertyName("studentId")]
    public Ulid StudentId { get; init; }

    [JsonPropertyName("student")]
    public UserDTO? Student { get; init; }

    [JsonPropertyName("assignerId")]
    public Ulid? AssignerId { get; init; }

    [JsonPropertyName("assigner")]
    public UserDTO? Assigner { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
