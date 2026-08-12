using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Media.DTO;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

public record PollAnswerDTO : IHasPresignedMedia
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "PollAnswer";

    public IEnumerable<MediaDTO?> GetMediaForPresigning()
    {
        return PresignedMedia.Collect(Medias);
    }

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("pollQuestionId")]
    public Ulid PollQuestionId { get; init; }

    [JsonPropertyName("value")]
    public PollAnswerValue? Value { get; init; }

    /// <summary>
    /// Files attached to the answer. Only a <see cref="PollQuestionType.Files"/>
    /// question ever has them.
    /// </summary>
    [Required]
    [JsonPropertyName("medias")]
    public IEnumerable<MediaDTO> Medias { get; init; } = [];

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }
}
