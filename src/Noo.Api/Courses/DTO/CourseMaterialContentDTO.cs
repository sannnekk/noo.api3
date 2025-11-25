using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Media.DTO;
using Noo.Api.NooTube.DTO;
using Noo.Api.Polls.DTO;

namespace Noo.Api.Courses.DTO;

public record CourseMaterialContentDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "CourseMaterialContent";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("content")]
    public IRichTextType Content { get; init; } = default!;

    [JsonPropertyName("poll")]
    public PollDTO? Poll { get; init; }

    [Required]
    [JsonPropertyName("nooTubeVideos")]
    public IEnumerable<NooTubeVideoDTO> NooTubeVideos { get; init; } = [];

    [Required]
    [JsonPropertyName("medias")]
    public IEnumerable<MediaDTO> Medias { get; init; } = [];

    [Required]
    [JsonPropertyName("workAssignments")]
    public IEnumerable<CourseWorkAssignmentDTO> WorkAssignments { get; init; } = [];

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
