using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.Models;
using Noo.Api.Users.Models;

namespace Noo.Api.Courses.Services;

public class CourseMembershipRepository : Repository<CourseMembershipModel>, ICourseMembershipRepository
{
    public Task<CourseMembershipModel?> GetMembershipAsync(Ulid courseId, Ulid userId)
    {
        return Context.Set<CourseMembershipModel>()
            .Where(m => m.CourseId == courseId && m.StudentId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<UserModel>> GetUsersByCourseIdAsync(Ulid courseId)
    {
        return await Context.Set<CourseMembershipModel>()
            .Where(m => m.CourseId == courseId)
            .Select(m => m.Student)
            .AsNoTracking()
            .ToListAsync();
    }
}

public static class CourseMembershipRepositoryExtensions
{
    public static ICourseMembershipRepository CourseMembershipRepository(this IUnitOfWork unitOfWork)
    {
        return new CourseMembershipRepository
        {
            Context = unitOfWork.Context
        };
    }
}
