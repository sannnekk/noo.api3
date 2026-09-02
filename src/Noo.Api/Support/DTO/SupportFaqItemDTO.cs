using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Validation.Attributes;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.DTO;

public record SupportFaqItemDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "SupportFaqItem";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; set; }

    [Required]
    [JsonPropertyName("order")]
    [Range(0, 255)]
    public int Order { get; set; }

    [Required]
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("answer")]
    [RichText(AllowEmpty = false, AllowNull = false)]
    public IRichTextType Answer { get; set; } = default!;

    [Required]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("category")]
    public SupportCategory? Category { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
