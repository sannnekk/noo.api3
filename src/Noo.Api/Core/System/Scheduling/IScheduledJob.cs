namespace Noo.Api.Core.System.Scheduling;

public interface IScheduledJob
{
    public TimeSpan Interval { get; }

    public Task RunAsync(CancellationToken cancellationToken);
}
