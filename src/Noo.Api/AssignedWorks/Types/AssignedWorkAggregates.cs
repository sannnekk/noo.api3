namespace Noo.Api.AssignedWorks.Types;

// Both types use init-only properties rather than positional parameters on purpose: EF projects
// a grouping into a member initializer happily, but cannot translate one projected into a
// constructor call — something the InMemory provider used in tests accepts and MySQL rejects.

/// <summary>
/// How many assigned works fall on one day, as aggregated by the database.
/// </summary>
public record AssignedWorkDayCount
{
    public DateTime Day { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// The average score of one month's assigned works, as aggregated by the database.
/// </summary>
public record AssignedWorkMonthAverage
{
    public int Year { get; init; }
    public int Month { get; init; }
    public double? AverageScore { get; init; }
}
