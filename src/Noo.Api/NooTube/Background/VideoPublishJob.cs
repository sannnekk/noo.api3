using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Scheduling;
using Noo.Api.Core.Utils;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Background;

[RegisterScheduledJob]
public class VideoPublishingJob : IScheduledJob
{
    private readonly NooDbContext _db;

    public VideoPublishingJob(NooDbContext db)
    {
        _db = db;
    }

    public TimeSpan Interval => NooTubeConfig.BackgroundJobInterval;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = Clock.Now;

        await _db.Set<NooTubeVideoModel>()
            .Where(c => !c.IsActive && c.PublishedAt != null && c.PublishedAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, true), cancellationToken);
    }
}
