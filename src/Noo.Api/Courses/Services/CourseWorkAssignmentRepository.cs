using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseWorkAssignmentRepository))]
public class CourseWorkAssignmentRepository
    : Repository<CourseWorkAssignmentModel>,
        ICourseWorkAssignmentRepository
{
    public CourseWorkAssignmentRepository(NooDbContext dbContext)
        : base(dbContext) { }

    public Task<CourseWorkAssignmentModel> GetWithWorkAsync(Ulid workAssignmentId)
    {
        return Context
            .GetDbSet<CourseWorkAssignmentModel>()
            .Include(a => a.Work)
            .FirstAsync(a => a.Id == workAssignmentId);
    }
}
