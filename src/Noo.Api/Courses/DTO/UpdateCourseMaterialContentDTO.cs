using System.Text.Json.Serialization;
using Noo.Api.Core.Request;
using Noo.Api.Core.Utils.Richtext;

namespace Noo.Api.Courses.DTO;

public record UpdateCourseMaterialContentDTO
{
    [JsonPropertyName("content")]
    public IRichTextType? Content { get; init; }

    [JsonPropertyName("pollId")]
    public Ulid? PollId { get; init; }

    [JsonPropertyName("nootubeVideos")]
    public IDictionary<string, IdReferenceDTO>? NooTubeVideos { get; init; }

    [JsonPropertyName("medias")]
    public IDictionary<string, IdReferenceDTO>? Medias { get; init; }

    [JsonPropertyName("workAssignments")]
    public IDictionary<string, UpdateCourseWorkAssignmentDTO>? WorkAssignments { get; init; }
}
