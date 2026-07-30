using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Noo.Api.Polls.Types;

namespace Noo.Api.Polls.DTO;

public record CreatePollQuestionDTO
{
    [JsonPropertyName("title")]
    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [MaxLength(512)]
    public string? Description { get; init; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; init; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("type")]
    [Required]
    public PollQuestionType Type { get; init; }

    [JsonPropertyName("config")]
    public PollQuestionConfig Config { get; init; } = default!;
}
