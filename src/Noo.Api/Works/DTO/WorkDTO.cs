using System.Text.Json.Serialization;
using Noo.Api.Subjects.DTO;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record WorkDTO
{
    [JsonPropertyName("_entityName")]
    public string EntityName => "Work";

    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

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
