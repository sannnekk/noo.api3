using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;

namespace Noo.Api.Courses.DTO;

public record CreateCourseMaterialContentDTO
{
    [JsonPropertyName("content")]
    [Required]
    public IRichTextType Content { get; init; } = default!;

    [JsonPropertyName("pollId")]
    public Ulid? PollId { get; init; }

    [JsonPropertyName("nootubeVideoIds")]
    public IEnumerable<Ulid> NooTubeVideoIds { get; init; } = [];

    [JsonPropertyName("mediaIds")]
    public IEnumerable<Ulid> MediaIds { get; init; } = [];

    [JsonPropertyName("workAssignments")]
    public IEnumerable<CreateCourseWorkAssignmentDTO> WorkAssignments { get; init; } = [];
}
