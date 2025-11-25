using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

public record PollQuestionDTO
{
    [Required]
    [JsonPropertyName("_entityName")]
    public string EntityName => "PollQuestion";

    [Required]
    [JsonPropertyName("id")]
    public Ulid Id { get; init; }

    [Required]
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [Required]
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; init; }

    [Required]
    [JsonPropertyName("type")]
    public PollQuestionType Type { get; init; }

    [JsonPropertyName("config")]
    public PollQuestionConfig? Config { get; init; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}
