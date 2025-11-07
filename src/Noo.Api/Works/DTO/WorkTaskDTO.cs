using System.Text.Json.Serialization;
using Cysharp.Serialization.Json;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record WorkTaskDTO
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(UlidJsonConverter))]
    public Ulid? Id { get; init; }

    [JsonPropertyName("type")]
    public WorkTaskType Type { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [JsonPropertyName("content")]
    public IRichTextType Content { get; set; } = default!;

    [JsonPropertyName("rightAnswer")]
    public IEnumerable<string>? RightAnswers { get; set; }

    [JsonPropertyName("solveHint")]
    public IRichTextType? SolveHint { get; set; }

    [JsonPropertyName("explanation")]
    public IRichTextType? Explanation { get; set; }

    [JsonPropertyName("checkStrategy")]
    public WorkTaskCheckStrategy CheckStrategy { get; set; } = WorkTaskCheckStrategy.Manual;

    [JsonPropertyName("showAnswerBeforeCheck")]
    public bool ShowAnswerBeforeCheck { get; set; } = false;

    [JsonPropertyName("checkOneByOne")]
    public bool CheckOneByOne { get; set; } = false;
}
