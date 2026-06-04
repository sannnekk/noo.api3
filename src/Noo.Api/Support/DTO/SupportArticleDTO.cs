using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Validation.Attributes;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.DTO;

public record SupportArticleDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "SupportArticle";

    [Required]
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("order")]
    [Range(0, 255)]
    public int Order { get; set; }

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("content")]
    [RichText(AllowEmpty = false, AllowNull = false)]
    public IRichTextType Content { get; set; } = default!;

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [Required]
    [JsonPropertyName("category")]
    public SupportCategory Category { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
