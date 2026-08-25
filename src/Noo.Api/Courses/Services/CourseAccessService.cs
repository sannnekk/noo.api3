using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Access;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseAccessService))]
public class CourseAccessService : ICourseAccessService
{
    private readonly NooDbContext _db;

    public CourseAccessService(NooDbContext db)
    {
        _db = db;
    }

    public Task<bool> HasAccessAsync(Ulid courseId, Ulid studentId)
    {
        return _db.GetDbSet<CourseModel>()
            .Where(c => c.Id == courseId)
            .AnyAsync(CourseAccessRules.AccessibleBy(studentId));
    }
}
