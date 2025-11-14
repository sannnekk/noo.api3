using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cysharp.Serialization.Json;
using Noo.Api.Core.Utils.Richtext;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.DTO;

public record WorkTaskDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "WorkTask";

    [Required]
    [JsonPropertyName("id")]
    [JsonConverter(typeof(UlidJsonConverter))]
    public Ulid? Id { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public WorkTaskType Type { get; set; }

    [Required]
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [Required]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [Required]
    [JsonPropertyName("content")]
    public IRichTextType Content { get; set; } = default!;

    [JsonPropertyName("rightAnswers")]
    public IEnumerable<string>? RightAnswers { get; set; }

    [JsonPropertyName("solveHint")]
    public IRichTextType? SolveHint { get; set; }

    [JsonPropertyName("explanation")]
    public IRichTextType? Explanation { get; set; }

    [Required]
    [JsonPropertyName("checkStrategy")]
    public WorkTaskCheckStrategy CheckStrategy { get; set; } = WorkTaskCheckStrategy.Manual;

    [JsonPropertyName("showAnswerBeforeCheck")]
    public bool ShowAnswerBeforeCheck { get; set; }

    [JsonPropertyName("checkOneByOne")]
    public bool CheckOneByOne { get; set; }
}
