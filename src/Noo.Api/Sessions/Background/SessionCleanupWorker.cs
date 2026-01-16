using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Sessions.Models;
using Microsoft.Extensions.Options;

namespace Noo.Api.Sessions.Background;

public class SessionCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly SessionConfig _options;

    public SessionCleanupWorker(IServiceProvider services, IOptions<SessionConfig> options)
    {
        _services = services;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NooDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-_options.SessionRetentionDays);
                await db.Set<SessionModel>()
                    .Where(s => (s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt) < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch
            {
                // ignore errors, retry later
            }

            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
