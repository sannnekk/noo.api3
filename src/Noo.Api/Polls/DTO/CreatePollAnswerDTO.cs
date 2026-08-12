using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

public record CreatePollAnswerDTO
{
    [JsonPropertyName("pollQuestionId")]
    [Required]
    public Ulid PollQuestionId { get; init; }

    [JsonPropertyName("value")]
    public PollAnswerValue? Value { get; init; }

    /// <summary>
    /// Files answering a <see cref="PollQuestionType.Files"/> question. They are
    /// uploaded before the poll is submitted, so the answer only carries their ids.
    /// </summary>
    [JsonPropertyName("mediaIds")]
    public IEnumerable<Ulid> MediaIds { get; init; } = [];
}
