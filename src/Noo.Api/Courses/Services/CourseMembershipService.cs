using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.DTO;
using Noo.Api.Courses.Events;
using Noo.Api.Courses.Filters;
using Noo.Api.Courses.Models;
using Noo.Api.Courses.QuerySpecifications;

namespace Noo.Api.Courses.Services;

[RegisterScoped(typeof(ICourseMembershipService))]
public class CourseMembershipService : ICourseMembershipService
{
    private readonly ICourseMembershipRepository _courseMembershipRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    private readonly IEventPublisher _events;

    public CourseMembershipService(
        ICourseMembershipRepository courseMembershipRepository,
        IMapper mapper,
        ICurrentUser currentUser,
        IEventPublisher events
    )
    {
        _courseMembershipRepository = courseMembershipRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _events = events;
    }

    public async Task<Ulid> CreateMembershipAsync(CreateCourseMembershipDTO dto)
    {
        var model = _mapper.Map<CourseMembershipModel>(dto);

        model.AssignerId = _currentUser.UserId;
        _courseMembershipRepository.Add(model);

        await _events.PublishAsync(
            new CourseMembershipCreatedEvent(
                model.Id,
                model.StudentId,
                model.CourseId,
                model.AssignerId
            )
        );

        return model.Id;
    }

    public Task<CourseMembershipModel?> GetMembershipAsync(Ulid courseId, Ulid userId)
    {
        return _courseMembershipRepository.GetMembershipAsync(courseId, userId);
    }

    public Task<CourseMembershipModel?> GetMembershipByIdAsync(Ulid membershipId)
    {
        return _courseMembershipRepository.GetByIdAsync(membershipId);
    }

    public Task<SearchResult<CourseMembershipModel>> GetMembershipsAsync(
        CourseMembershipFilter filter,
        Ulid? userId = null
    )
    {
        return _courseMembershipRepository.SearchAsync(
            filter,
            [
                new CourseMembershipSpecification(
                    _currentUser.UserRole,
                    userId.HasValue ? userId.Value : _currentUser.UserId
                ),
            ]
        );
    }

    public async Task<bool> HasAccessAsync(Ulid courseId, Ulid userId)
    {
        var membership = await _courseMembershipRepository.GetMembershipAsync(courseId, userId);

        return membership != null && membership.IsActive;
    }

    public async Task SoftDeleteMembershipAsync(Ulid membershipId)
    {
        var membership = await _courseMembershipRepository.GetByIdAsync(membershipId);

        if (membership == null)
            return;

        membership.IsActive = false;

        await _events.PublishAsync(
            new CourseMembershipRemovedEvent(
                membership.Id,
                membership.StudentId,
                membership.CourseId,
                _currentUser.UserId
            )
        );
    }

    public async Task SetArchivedByStudentAsync(Ulid membershipId, bool isArchived)
    {
        var membership = await GetOwnMembershipAsync(membershipId);

        membership.IsArchivedByStudent = isArchived;
    }

    public async Task SetPinnedByStudentAsync(Ulid membershipId, bool isPinned)
    {
        var membership = await GetOwnMembershipAsync(membershipId);

        membership.IsPinnedByStudent = isPinned;
    }

    private async Task<CourseMembershipModel> GetOwnMembershipAsync(Ulid membershipId)
    {
        var membership = await _courseMembershipRepository.GetByIdAsync(membershipId);

        if (membership == null || membership.StudentId != _currentUser.UserId)
        {
            throw new NotFoundException();
        }

        return membership;
    }
}
