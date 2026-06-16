using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Noo.Api.Works.DTO;

public record WorkStatisticsDTO
{
    [Required]
    [JsonPropertyName("taskSummaries")]
    public IEnumerable<TaskSummaryDTO> TaskSummaries { get; init; } = [];

    [Required]
    [JsonPropertyName("averageWorkScore")]
    public ScoreSummaryDTO AverageWorkScore { get; init; } = new();

    [Required]
    [JsonPropertyName("medianWorkScore")]
    public ScoreSummaryDTO MedianWorkScore { get; init; } = new();

    [Required]
    [JsonPropertyName("workSolveCount")]
    public int WorkSolveCount { get; init; }

    [Required]
    [JsonPropertyName("work")]
    public WorkDTO Work { get; init; } = default!;
}
