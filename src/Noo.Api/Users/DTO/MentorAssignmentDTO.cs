using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;
using Noo.Api.Subjects.DTO;

namespace Noo.Api.Users.DTO;

public record MentorAssignmentDTO : IHasPresignedMedia
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "MentorAssignment";

    /// <summary>
    /// Both sides of an assignment are shown with their avatar, which has to be
    /// presigned when it is an uploaded file rather than a Telegram picture.
    /// </summary>
    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(Mentor, Student);
    }

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
