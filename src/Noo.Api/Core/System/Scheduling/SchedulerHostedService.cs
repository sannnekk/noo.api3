namespace Noo.Api.Core.System.Scheduling;

public class SchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ScheduledJobRegistry _registry;
    private readonly ILogger<SchedulerHostedService> _logger;

    public SchedulerHostedService(
        IServiceProvider services,
        ScheduledJobRegistry registry,
        ILogger<SchedulerHostedService> logger)
    {
        _services = services;
        _registry = registry;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loops = _registry.JobTypes.Select(type => RunLoopAsync(type, stoppingToken));
        return Task.WhenAll(loops);
    }

    private async Task RunLoopAsync(Type jobType, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(GetInterval(jobType));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var job = (IScheduledJob)scope.ServiceProvider.GetRequiredService(jobType);
                await job.RunAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Scheduled job {Job} failed.", jobType.Name);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private TimeSpan GetInterval(Type jobType)
    {
        using var scope = _services.CreateScope();
        return ((IScheduledJob)scope.ServiceProvider.GetRequiredService(jobType)).Interval;
    }
}
