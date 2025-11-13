using System.Text.Json.Serialization;

namespace Noo.Api.Subjects.DTO;

public record SubjectDTO
{
    [JsonPropertyName("_entityName")]
    public string EntityName => "Subject";

    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
