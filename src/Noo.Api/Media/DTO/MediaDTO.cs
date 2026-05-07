using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Media.Types;

namespace Noo.Api.Media.DTO;

public record MediaDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "Media";

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

    [Required]
    [JsonPropertyName("category")]
    public MediaCategory Category { get; set; }

    [Required]
    [JsonPropertyName("status")]
    public MediaStatus Status { get; set; }

    [JsonPropertyName("entityId")]
    public Ulid? EntityId { get; set; }

    [Required]
    [JsonPropertyName("ownerId")]
    public Ulid OwnerId { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
