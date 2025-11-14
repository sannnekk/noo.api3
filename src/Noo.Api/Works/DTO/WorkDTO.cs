using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record WorkDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "Work";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("type")]
    public WorkType Type { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("subjectId")]
    public Ulid? SubjectId { get; init; }

    [JsonPropertyName("subject")]
    public SubjectDTO? Subject { get; init; }

    [JsonPropertyName("tasks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<WorkTaskDTO> Tasks { get; init; } = [];
}
