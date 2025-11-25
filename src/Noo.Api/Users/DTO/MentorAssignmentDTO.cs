using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Subjects.DTO;

namespace Noo.Api.Users.DTO;

public record MentorAssignmentDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "MentorAssignment";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("studentId")]
    public Ulid StudentId { get; set; }

    [JsonPropertyName("student")]
    public UserDTO? Student { get; set; }

    [Required]
    [JsonPropertyName("mentorId")]
    public Ulid MentorId { get; set; }

    [JsonPropertyName("mentor")]
    public UserDTO? Mentor { get; set; }

    [Required]
    [JsonPropertyName("subjectId")]
    public Ulid SubjectId { get; set; }

    [JsonPropertyName("subject")]
    public SubjectDTO? Subject { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
