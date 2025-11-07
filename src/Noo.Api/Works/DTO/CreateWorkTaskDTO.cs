using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Core.Validation.Attributes;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record CreateWorkTaskDTO
{
    [Required]
    [JsonPropertyName("type")]
    public WorkTaskType Type { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [Required]
    [RichText(AllowEmpty = false)]
    [JsonPropertyName("content")]
    public IRichTextType Content { get; set; } = default!;

    [JsonPropertyName("rightAnswers")]
    public IEnumerable<string>? RightAnswers { get; set; }

    [RichText(AllowEmpty = true, AllowNull = true)]
    [JsonPropertyName("solveHint")]
    public IRichTextType? SolveHint { get; set; }

    [RichText(AllowEmpty = true, AllowNull = true)]
    [JsonPropertyName("explanation")]
    public IRichTextType? Explanation { get; set; }

    [JsonPropertyName("checkStrategy")]
    public WorkTaskCheckStrategy CheckStrategy { get; set; } = WorkTaskCheckStrategy.Manual;

    [JsonPropertyName("showAnswerBeforeCheck")]
    public bool ShowAnswerBeforeCheck { get; set; }

    [JsonPropertyName("checkOneByOne")]
    public bool CheckOneByOne { get; set; }
}
