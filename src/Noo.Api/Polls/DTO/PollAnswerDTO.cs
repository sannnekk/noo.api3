using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

public record PollAnswerDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "PollAnswer";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("pollQuestionId")]
    public Ulid PollQuestionId { get; init; }

    [JsonPropertyName("value")]
    public PollAnswerValue? Value { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}
