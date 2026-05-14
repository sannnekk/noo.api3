using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Utils.Richtext;

namespace Noo.Api.AssignedWorks.DTO;

public record AssignedWorkAnswerDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "AssignedWorkAnswer";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [JsonPropertyName("richTextContent")]
    public IRichTextType? RichTextContent { get; init; }

    [JsonPropertyName("wordContent")]
    public string? WordContent { get; init; }

    [JsonPropertyName("mentorComment")]
    public IRichTextType? MentorComment { get; init; }

    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [Required]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; init; }

    [Required]
    [JsonPropertyName("status")]
    public AssignedWorkAnswerStatus Status { get; init; }

    [JsonPropertyName("detailedScore")]
    public Dictionary<string, int>? DetailedScore { get; init; }

    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
