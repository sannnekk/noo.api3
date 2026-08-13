using System.Text.Json;
using System.Text.Json.Serialization;
using Noo.Api.Core.Security.Authorization;

namespace Noo.Api.GoogleSheetsIntegrations.Exports;

/// <summary>
/// Everything an export needs to select its data. Kept as one flat structure — like
/// <see cref="Noo.Api.Polls.Types.PollQuestionConfig"/> — rather than a polymorphic hierarchy,
/// because it is persisted as JSON and each profile only reads the members it cares about.
/// Which members are required (or mutually exclusive) is enforced by the profile's
/// <c>Validate</c>, not by the type.
/// </summary>
public struct ExportParameters
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

    public static ExportParameters Deserialize(string v)
    {
        return JsonSerializer.Deserialize<ExportParameters>(v);
    }

    public readonly string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
