using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record CreateWorkDTO
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("type")]
    public WorkType Type { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [JsonPropertyName("subjectId")]
    public Ulid? SubjectId { get; set; }

    [Required]
    [MinLength(1)]
    // No [ValidateEnumeratedItems] here: it is an Microsoft.Extensions.Options attribute read by
    // the options source generator, inert under MVC validation. Per-task rules are reached by
    // MVC's own graph walk, which recurses into collection items anyway.
    [MaxLength(300)]
    [JsonPropertyName("tasks")]
    public ICollection<CreateWorkTaskDTO> Tasks { get; set; } = [];
}
