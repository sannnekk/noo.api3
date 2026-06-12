using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Noo.Api.Auth.Models;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Scheduling;
using Noo.Api.Core.Utils;
using Noo.Api.Sessions.Models;

namespace Noo.Api.Sessions.Background;

[RegisterScheduledJob]
public class SessionCleanupJob : IScheduledJob
{
    private readonly NooDbContext _db;
    private readonly SessionConfig _options;

    public SessionCleanupJob(NooDbContext db, IOptions<SessionConfig> options)
    {
        _db = db;
        _options = options.Value;
    }

    public TimeSpan Interval => _options.CleanupInterval;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = Clock.Now.AddDays(-_options.SessionRetentionDays);

        await _db.Set<SessionModel>()
            .Where(s => (s.LastRequestAt ?? s.UpdatedAt ?? s.CreatedAt) < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.Set<RefreshTokenModel>()
            .Where(t => t.ExpiresAt < Clock.Now)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
