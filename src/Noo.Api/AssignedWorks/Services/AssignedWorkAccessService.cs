using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkAccessService))]
public class AssignedWorkAccessService : IAssignedWorkAccessService
{
    private readonly ICurrentUser _currentUser;

    public AssignedWorkAccessService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Staff oversee every work; a student or a mentor only reaches the ones they are on.
    /// </summary>
    public bool CanRead(AssignedWorkModel assignedWork)
    {
        var (userId, userRole) = GetUserInfo();

        return userRole switch
        {
            UserRoles.Admin or UserRoles.Teacher or UserRoles.Assistant => true,
            UserRoles.Student or UserRoles.Mentor => assignedWork.IsParticipant(userId),
            _ => throw new UnauthorizedException(),
        };
    }

    /// <summary>
    /// A work can only be taken back before it has been handed in.
    /// </summary>
    public bool CanDelete(AssignedWorkModel assignedWork)
    {
        var (userId, userRole) = GetUserInfo();

        return userRole switch
        {
            UserRoles.Admin => true,
            UserRoles.Student =>
                assignedWork.StudentId == userId
                && AssignedWorkStatuses.Unsolved.Contains(assignedWork.SolveStatus),
            UserRoles.Teacher or UserRoles.Assistant or UserRoles.Mentor => false,
            _ => throw new UnauthorizedException(),
        };
    }

    /// <summary>
    /// Archiving is per role — staff put a work out of their own way whoever is on it,
    /// a student or a mentor only their own.
    /// </summary>
    public bool CanArchive(AssignedWorkModel assignedWork) => CanRead(assignedWork);

    public bool CanAssignMainMentor(AssignedWorkModel assignedWork)
    {
        var (_, userRole) = GetUserInfo();

        return !assignedWork.IsChecked
            && userRole switch
            {
                UserRoles.Admin or UserRoles.Teacher or UserRoles.Assistant => true,
                UserRoles.Student or UserRoles.Mentor => false,
                _ => throw new UnauthorizedException(),
            };
    }

    public bool CanAssignHelperMentor(AssignedWorkModel assignedWork)
    {
        var (userId, userRole) = GetUserInfo();

        return !assignedWork.IsChecked
            && userRole switch
            {
                UserRoles.Admin or UserRoles.Teacher => true,
                UserRoles.Mentor => assignedWork.IsParticipant(userId),
                UserRoles.Student or UserRoles.Assistant => false,
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
