using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Media.DTO;

public record MediaDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName { get; init; } = "Media";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("actualName")]
    public string ActualName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("extension")]
    public string Extension { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [Required]
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("entityId")]
    public Ulid? EntityId { get; set; }

    [JsonPropertyName("ownerId")]
    public Ulid? OwnerId { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
