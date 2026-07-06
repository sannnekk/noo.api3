using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseMaterialReactionRepository : IRepository<CourseMaterialReactionModel>
{
    public Task<CourseMaterialReactionModel?> GetAsync(Ulid materialId, Ulid userId);
}
