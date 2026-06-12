namespace Noo.Api.Core.System.Scheduling;

public sealed class ScheduledJobRegistry
{
    public IReadOnlyList<Type> JobTypes { get; }

    public ScheduledJobRegistry(IEnumerable<Type> jobTypes)
    {
        JobTypes = jobTypes.ToList();
    }
}
