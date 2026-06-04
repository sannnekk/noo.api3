using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Validation.Attributes;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.DTO;

public record CreateSupportArticleDTO
{
    [JsonPropertyName("title")]
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    [Required]
    [Range(0, 255)]
    public int Order { get; set; }

    [JsonPropertyName("content")]
    [RichText(AllowEmpty = false, AllowNull = false)]
    public IRichTextType Content { get; set; } = default!;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("category")]
    public SupportCategory Category { get; set; }
}
