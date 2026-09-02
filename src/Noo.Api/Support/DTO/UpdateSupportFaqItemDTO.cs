using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Validation.Attributes;
using Noo.Api.Support.Types;

namespace Noo.Api.Support.DTO;

public record UpdateSupportFaqItemDTO
{
    [JsonPropertyName("question")]
    [MaxLength(255)]
    public string? Question { get; set; }

    [JsonPropertyName("order")]
    [Range(0, 255)]
    public int? Order { get; set; }

    /// <summary>
    /// Nulling the answer is rejected rather than mapped: a FAQ item without one
    /// is not a state the reader can be shown.
    /// </summary>
    [JsonPropertyName("answer")]
    [RichText(AllowEmpty = false, AllowNull = false)]
    public IRichTextType? Answer { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Patching this to null is meaningful — it detaches the item from a
    /// category, which is how a general question is expressed.
    /// </summary>
    [JsonPropertyName("category")]
    public SupportCategory? Category { get; set; }
}
