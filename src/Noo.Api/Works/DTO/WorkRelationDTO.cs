using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Courses.DTO;
using Noo.Api.Subjects.DTO;

namespace Noo.Api.Works.Types;

public record WorkRelationDTO
{
    [Required]
    [JsonPropertyName("courseId")]
    public Ulid CourseId { get; set; }

    [Required]
    [JsonPropertyName("materialId")]
    public Ulid MaterialId { get; set; }

    [JsonPropertyName("subject")]
    public SubjectDTO? Subject { get; set; }

    [Required]
    [JsonPropertyName("path")]
    public IEnumerable<string> Path { get; set; } = null!;

    [Required]
    [JsonPropertyName("assignment")]
    public CourseWorkAssignmentDTO Assignment { get; set; } = null!;
}
