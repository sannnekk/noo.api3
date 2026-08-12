using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

public record UpdatePollAnswerDTO
{
    [JsonPropertyName("value")]
    public PollAnswerValue Value { get; init; }

    /// <summary>
    /// Files answering a <see cref="PollQuestionType.Files"/> question, as the whole
    /// list the answer should end up with. Uploading is a separate step, so only the
    /// ids travel here.
    /// </summary>
    [JsonPropertyName("mediaIds")]
    public IEnumerable<Ulid> MediaIds { get; init; } = [];
}
