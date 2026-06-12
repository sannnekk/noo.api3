using System.Reflection;
using Noo.Api.Core.System.Scheduling;

namespace Noo.Api.Core.Initialization.ServiceCollection;

public static class HostedServicesExtension
{
    public static void AddHostedServices(this IServiceCollection services)
    {
        var jobTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<RegisterScheduledJobAttribute>() != null)
            .ToList();

        foreach (var jobType in jobTypes)
        {
            if (!typeof(IScheduledJob).IsAssignableFrom(jobType))
            {
                throw new InvalidOperationException($"Scheduled job type {jobType.FullName} must implement IScheduledJob.");
            }

            services.AddScoped(jobType);
        }

        services.AddSingleton(new ScheduledJobRegistry(jobTypes));
        services.AddHostedService<SchedulerHostedService>();
    }
}
