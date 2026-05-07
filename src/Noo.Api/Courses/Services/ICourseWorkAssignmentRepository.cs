using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseWorkAssignmentRepository : IRepository<CourseWorkAssignmentModel>
{
    public Task<CourseWorkAssignmentModel> GetWithWorkAsync(Ulid workAssignmentId);
}
