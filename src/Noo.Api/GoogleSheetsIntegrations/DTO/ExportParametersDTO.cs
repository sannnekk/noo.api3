using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.GoogleSheetsIntegrations.DTO;

/// <summary>
/// Selection criteria for an export. Which members are required depends on the export type and
/// is validated by that type's profile.
/// </summary>
public record ExportParametersDTO
{
    [JsonPropertyName("role")]
    public UserRoles? Role { get; set; }

    [JsonPropertyName("courseId")]
    public Ulid? CourseId { get; set; }

    [JsonPropertyName("subjectId")]
    public Ulid? SubjectId { get; set; }

    [JsonPropertyName("createdFrom")]
    public DateTime? CreatedFrom { get; set; }

    [JsonPropertyName("createdTo")]
    public DateTime? CreatedTo { get; set; }

    [JsonPropertyName("pollId")]
    public Ulid? PollId { get; set; }

    [JsonPropertyName("studentId")]
    public Ulid? StudentId { get; set; }

    [JsonPropertyName("mentorId")]
    public Ulid? MentorId { get; set; }
}
