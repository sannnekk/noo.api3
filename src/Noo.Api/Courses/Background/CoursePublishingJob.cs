using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Scheduling;
using Noo.Api.Core.Utils;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Background;

[RegisterScheduledJob]
public class CoursePublishingJob : IScheduledJob
{
    private readonly NooDbContext _db;

    public CoursePublishingJob(NooDbContext db)
    {
        _db = db;
    }

    public TimeSpan Interval => CourseConfig.BackgroundJobInterval;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = Clock.Now;

        await _db.Set<CourseChapterModel>()
            .Where(c => !c.IsActive && c.PublishAt != null && c.PublishAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, true), cancellationToken);

        await _db.Set<CourseMaterialModel>()
            .Where(m => !m.IsActive && m.PublishAt != null && m.PublishAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsActive, true), cancellationToken);
    }
}
