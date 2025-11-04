using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.DTO;

namespace Noo.Api.Courses.Models;

/// <summary>
/// Resolves the MemberCount field for a course only when the current user
/// is authorized (Teacher or Admin). Returns null otherwise
/// </summary>
[RegisterScoped(typeof(IValueResolver<CourseModel, CourseDTO, int?>))]
public class CourseMemberCountValueResolver : IValueResolver<CourseModel, CourseDTO, int?>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CourseMemberCountValueResolver(ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public int? Resolve(CourseModel source, CourseDTO destination, int? destMember, ResolutionContext context)
    {
        if (!_currentUser.IsInRole(UserRoles.Admin, UserRoles.Teacher))
        {
            return null;
        }

        // If memberships were eagerly loaded we can count without another query
        if (source.Memberships?.Count > 0)
        {
            // Prefer counting only active & not archived memberships if those flags exist
            return source.Memberships.Count(m => m.IsActive);
        }

        // Fallback: lightweight COUNT(*) query (no tracking) filtered for active memberships
        return _unitOfWork.Context.Set<CourseMembershipModel>()
            .AsNoTracking()
            .Where(m => m.CourseId == source.Id && m.IsActive && !m.IsArchived)
            .Count();
    }
}
