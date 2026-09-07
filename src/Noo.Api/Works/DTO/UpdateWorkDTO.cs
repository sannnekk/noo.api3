using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record UpdateWorkDTO
{
    [JsonPropertyName("id")]
    public Ulid? Id { get; set; }

    [MinLength(1)]
    [MaxLength(200)]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [Required]
    [JsonPropertyName("type")]
    public WorkType? Type { get; set; }

    [MaxLength(255)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("subjectId")]
    public Ulid? SubjectId { get; set; }

    // The cap has to be repeated here: POST validates the collection it receives, but a PATCH
    // that adds tasks one operation at a time never presents a whole collection to validate.
    // Each task's own rules are reached by the graph walk in JsonPatchDocumentExtensions, which
    // is what gets past the dictionary; the count is the one thing it cannot see.
    [Required]
    [MaxLength(300)]
    [JsonPropertyName("tasks")]
    public IDictionary<string, UpdateWorkTaskDTO>? Tasks { get; set; }
}
