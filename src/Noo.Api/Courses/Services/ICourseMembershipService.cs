using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseMembershipService
{
    public Task<CourseMembershipModel?> GetMembershipAsync(Ulid courseId, Ulid userId);
    public Task<CourseMembershipModel?> GetMembershipByIdAsync(Ulid membershipId);
    public Task<SearchResult<CourseMembershipModel>> GetMembershipsAsync(
        CourseMembershipFilter filter,
        Ulid? userId = null
    );
    public Task<Ulid> CreateMembershipAsync(CreateCourseMembershipDTO dto);
    public Task SoftDeleteMembershipAsync(Ulid membershipId);
}
