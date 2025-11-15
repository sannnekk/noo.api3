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
}
