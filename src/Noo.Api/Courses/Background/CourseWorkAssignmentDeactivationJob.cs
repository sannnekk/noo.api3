using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.System.Scheduling;
using Noo.Api.Core.Utils;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Background;

[RegisterScheduledJob]
public class CourseWorkAssignmentDeactivationJob : IScheduledJob
{
    private readonly NooDbContext _db;

    public CourseWorkAssignmentDeactivationJob(NooDbContext db)
    {
        _db = db;
    }

    public TimeSpan Interval => CourseConfig.BackgroundJobInterval;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = Clock.Now;

        await _db.Set<CourseWorkAssignmentModel>()
            .Where(a => a.IsActive && a.DeactivatedAt != null && a.DeactivatedAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false), cancellationToken);
    }
}
