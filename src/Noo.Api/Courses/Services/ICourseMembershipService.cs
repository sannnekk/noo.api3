using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;

namespace Noo.Api.Courses.Services;

public interface ICourseMembershipService
{
    public Task<bool> HasAccessAsync(Ulid courseId, Ulid userId);
    public Task<CourseMembershipModel?> GetMembershipAsync(Ulid courseId, Ulid userId);
    public Task<CourseMembershipModel?> GetMembershipByIdAsync(Ulid membershipId);
    public Task<SearchResult<CourseMembershipModel>> GetMembershipsAsync(
        CourseMembershipFilter filter,
        Ulid? userId = null
    );
    public Ulid CreateMembership(CreateCourseMembershipDTO dto);
    public Task SoftDeleteMembershipAsync(Ulid membershipId);
}
