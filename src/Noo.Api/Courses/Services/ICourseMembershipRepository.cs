using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Users.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseMembershipRepository : IRepository<CourseMembershipModel>
{
    public Task<CourseMembershipModel?> GetMembershipAsync(Ulid courseId, Ulid userId);
    public Task<IEnumerable<UserModel>> GetUsersByCourseIdAsync(Ulid courseId);
}


