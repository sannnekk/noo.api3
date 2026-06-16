using System.Text.Json.Serialization;
using Noo.Api.Works.Models;

namespace Noo.Api.Works.Types;

public class WorkStatistics
{
    public IReadOnlyList<TaskSummary> TaskSummaries { get; init; } = [];

    public ScoreSummary AverageWorkScore { get; init; } = new();

    public ScoreSummary MedianWorkScore { get; init; } = new();

    public int WorkSolveCount { get; init; }

    [JsonIgnore]
    public WorkModel Work { get; set; } = default!;
}
