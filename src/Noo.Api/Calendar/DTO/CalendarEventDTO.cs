using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Calendar.Types;

namespace Noo.Api.Calendar.DTO;

public record CalendarEventDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "CalendarEvent";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [JsonPropertyName("assignedWorkId")]
    public Ulid? AssignedWorkId { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public CalendarEventType Type { get; set; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [Required]
    [JsonPropertyName("startDateTime")]
    public DateTime StartDateTime { get; init; }

    [JsonPropertyName("endDateTime")]
    public DateTime? EndDateTime { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}
