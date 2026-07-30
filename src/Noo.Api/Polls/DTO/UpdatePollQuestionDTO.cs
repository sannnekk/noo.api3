using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

/// <summary>
/// DTO to update a poll question.
/// </summary>
/// <remarks>Questions are patched through the Id-keyed
/// <see cref="UpdatePollDTO.Questions"/> dictionary: an entry keyed by an existing
/// question Id is updated in place, an unknown key creates a question with that key
/// as its Id, and a question missing from the dictionary is removed from the poll.
/// </remarks>
public record UpdatePollQuestionDTO
{
    [JsonPropertyName("id")]
    public Ulid? Id { get; init; }

    [JsonPropertyName("order")]
    public int? Order { get; init; }

    [JsonPropertyName("title")]
    [MaxLength(255)]
    public string? Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [MaxLength(512)]
    public string? Description { get; init; }

    [JsonPropertyName("isRequired")]
    public bool? IsRequired { get; init; }

    [JsonPropertyName("type")]
    public PollQuestionType? Type { get; init; }

    [JsonPropertyName("config")]
    public PollQuestionConfig? Config { get; init; }
}
