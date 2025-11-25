using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseContentRepository : IRepository<CourseMaterialContentModel>
{
    public Task<CourseMaterialContentModel?> GetAsync(Ulid contentId);
}
