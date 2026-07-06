using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseMaterialReactionRepository))]
public class CourseMaterialReactionRepository
    : Repository<CourseMaterialReactionModel>,
        ICourseMaterialReactionRepository
{
    public CourseMaterialReactionRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public Task<CourseMaterialReactionModel?> GetAsync(Ulid materialId, Ulid userId)
    {
        return Context
            .GetDbSet<CourseMaterialReactionModel>()
            .Where(r => r.MaterialId == materialId && r.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
