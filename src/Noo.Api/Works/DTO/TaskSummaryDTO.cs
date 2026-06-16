using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Works.DTO;

public record TaskSummaryDTO
{
    [Required]
    [JsonPropertyName("taskId")]
    public Ulid TaskId { get; init; }

    [JsonPropertyName("averageScore")]
    public double? AverageScore { get; init; }

    [Required]
    [JsonPropertyName("maxScore")]
    public int MaxScore { get; init; }
}
