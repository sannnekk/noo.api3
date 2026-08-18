using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkAccessService))]
public class AssignedWorkAccessService : IAssignedWorkAccessService
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly ICurrentUser _currentUser;

    public AssignedWorkAccessService(IAssignedWorkRepository assignedWorkRepository, ICurrentUser currentUser)
    {
        _assignedWorkRepository = assignedWorkRepository;
        _currentUser = currentUser;
    }

    public Task<bool> CanGetAssignedWorkAsync(Ulid assignedWorkId)
    {
        var (userId, userRole) = GetUserInfo();

        return userRole switch
        {
            UserRoles.Admin => Task.FromResult(true),
            UserRoles.Teacher => Task.FromResult(true),
            UserRoles.Assistant => Task.FromResult(true),
            UserRoles.Student => _assignedWorkRepository.IsStudentOwnWorkAsync(assignedWorkId, userId),
            UserRoles.Mentor => _assignedWorkRepository.IsMentorOwnWorkAsync(assignedWorkId, userId),
            _ => throw new UnauthorizedException(),
        };
    }

    public async Task<bool> CanSaveAssignedWorkAsync(Ulid assignedWorkId)
    {
        var (userId, userRole) = GetUserInfo();

        return userRole switch
        {
            UserRoles.Admin => true,
            UserRoles.Teacher => true,
            UserRoles.Assistant => false,
            UserRoles.Student =>
                await _assignedWorkRepository.IsStudentOwnWorkAsync(assignedWorkId, userId)
                && await _assignedWorkRepository.IsWorkSolveStatusAsync(assignedWorkId, AssignedWorkStatuses.Unsolved),
            // A mentor may write while the work is untouched or already handed in, but not
            // while the student is in the middle of solving it.
            UserRoles.Mentor =>
                await _assignedWorkRepository.IsMentorOwnWorkAsync(assignedWorkId, userId)
                && await _assignedWorkRepository.IsWorkSolveStatusAsync(assignedWorkId, [AssignedWorkSolveStatus.NotSolved, .. AssignedWorkStatuses.Solved])
                && await _assignedWorkRepository.IsWorkCheckStatusAsync(assignedWorkId, AssignedWorkStatuses.Unchecked),
            _ => throw new UnauthorizedException(),
        };
    }

    public async Task<bool> CanDeleteAssignedWorkAsync(Ulid assignedWorkId)
    {
        var (userId, userRole) = GetUserInfo();

        return userRole switch
        {
            UserRoles.Admin => true,
            UserRoles.Teacher => false,
            UserRoles.Assistant => false,
            UserRoles.Student =>
                await _assignedWorkRepository.IsStudentOwnWorkAsync(assignedWorkId, userId)
                && await _assignedWorkRepository.IsWorkSolveStatusAsync(assignedWorkId, AssignedWorkStatuses.Unsolved),
            UserRoles.Mentor => false,
            _ => throw new UnauthorizedException(),
        };
    }

    public async Task<bool> CanAddMainMentorAsync(Ulid assignedWorkId)
    {
        var (_, userRole) = GetUserInfo();

        if (!await _assignedWorkRepository.IsWorkCheckStatusAsync(assignedWorkId, AssignedWorkStatuses.Unchecked))
        {
            return false;
        }

        return userRole switch
        {
            UserRoles.Admin => true,
            UserRoles.Teacher => true,
            UserRoles.Assistant => true,
            UserRoles.Student => false,
            UserRoles.Mentor => false,
            _ => throw new UnauthorizedException(),
        };
    }

    public async Task<bool> CanAddHelperMentorAsync(Ulid assignedWorkId)
    {
        var (userId, userRole) = GetUserInfo();

        if (!await _assignedWorkRepository.IsWorkCheckStatusAsync(assignedWorkId, AssignedWorkStatuses.Unchecked))
        {
            return false;
        }

        return userRole switch
        {
            UserRoles.Admin => true,
            UserRoles.Teacher => true,
            UserRoles.Assistant => false,
            UserRoles.Student => false,
            UserRoles.Mentor => await _assignedWorkRepository.IsMentorOwnWorkAsync(assignedWorkId, userId),
            _ => throw new UnauthorizedException(),
        };
    }

    private (Ulid, UserRoles) GetUserInfo()
    {
        var userId = _currentUser.UserId;
        var userRole = _currentUser.UserRole;

        if (userId == null || userRole == null)
        {
            throw new UnauthorizedException();
        }

        return (userId.Value, userRole.Value);
    }
}
