using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseContentRepository))]
public class CourseContentRepository : Repository<CourseMaterialContentModel>, ICourseContentRepository
{
    public CourseContentRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<CourseMaterialContentModel?> GetAsync(Ulid contentId)
    {
        return Context.GetDbSet<CourseMaterialContentModel>()
            .Include(c => c.Medias)
            .Include(c => c.Poll)
            .Include(c => c.NooTubeVideos)
            .Include(c => c.WorkAssignments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == contentId);
    }
}
