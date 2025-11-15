using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.DTO;

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

    [JsonPropertyName("workId")]
    public Ulid? WorkId { get; init; }

    [Required]
    [JsonPropertyName("isWorkAvailable")]
    public bool IsWorkAvailable { get; init; }

    [JsonPropertyName("workSolveDeadlineAt")]
    public DateTime? WorkSolveDeadlineAt { get; init; }

    [JsonPropertyName("workCheckDeadlineAt")]
    public DateTime? WorkCheckDeadlineAt { get; init; }

    [JsonPropertyName("work")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkDTO? Work { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
