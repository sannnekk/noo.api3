using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Validation.Attributes;

namespace Noo.Api.AssignedWorks.DTO;

/// <summary>
/// Carries only the text: which of the work's three comments it lands in is decided
/// by the seat the caller holds on that work, not by anything they send.
/// </summary>
public record UpsertAssignedWorkCommentDTO
{
    [JsonPropertyName("content")]
    [RichText(AllowEmpty = true, AllowNull = true)]
    public IRichTextType? Content { get; set; }
}
