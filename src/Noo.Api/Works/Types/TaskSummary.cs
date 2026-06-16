namespace Noo.Api.Works.Types;

public class TaskSummary
{
    public Ulid TaskId { get; init; }

    public double? AverageScore { get; init; }

    public int MaxScore { get; init; }
}
