using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseMaterialContentDTO
{
    [JsonPropertyName("content")]
    public IRichTextType? Content { get; init; }

    [JsonPropertyName("pollId")]
    public Ulid? PollId { get; init; }

    [JsonPropertyName("nootubeVideoIds")]
    public IEnumerable<Ulid>? NooTubeVideoIds { get; init; }

    [JsonPropertyName("mediaIds")]
    public IEnumerable<Ulid>? MediaIds { get; init; }

    [JsonPropertyName("workAssignments")]
    public IEnumerable<UpdateCourseWorkAssignmentDTO>? WorkAssignments { get; init; }
}
