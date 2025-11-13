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

    [Required]
    //[MaxLength(300)]
    //[ValidateEnumeratedItems]
    [JsonPropertyName("tasks")]
    public IDictionary<string, UpdateWorkTaskDTO>? Tasks { get; set; }
}
