using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Support.DTO;

public record SupportCategoryDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "SupportCategory";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("parentId")]
    public Ulid? ParentId { get; set; }

    [Required]
    [JsonPropertyName("children")]
    public IEnumerable<SupportCategoryDTO> Children { get; set; } = [];

    [Required]
    [JsonPropertyName("articles")]
    public IEnumerable<SupportArticleDTO> Articles { get; set; } = [];

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
