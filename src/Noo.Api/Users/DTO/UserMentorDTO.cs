using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Users.DTO;

/// <summary>
/// A mentor of a student, as shown next to the student in the user list. Flat
/// and minimal on purpose: listings only name the mentor, link to them and
/// colour them by subject. Everything else about an assignment lives in
/// <see cref="MentorAssignmentDTO"/>.
/// </summary>
public record UserMentorDTO
{
    /// <summary>
    /// Id of the mentor, so listings can link to their profile.
    /// </summary>
    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subjectName")]
    public string? SubjectName { get; set; }

    [JsonPropertyName("subjectColor")]
    public string? SubjectColor { get; set; }
}
