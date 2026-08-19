using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Services;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkMentorService))]
public class AssignedWorkMentorService : IAssignedWorkMentorService
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAssignedWorkAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IEventPublisher _events;

    public AssignedWorkMentorService(
        IAssignedWorkRepository assignedWorkRepository,
        IUserRepository userRepository,
        IAssignedWorkAccessService access,
        ICurrentUser currentUser,
        IEventPublisher events
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _userRepository = userRepository;
        _access = access;
        _currentUser = currentUser;
        _events = events;
    }

    public async Task AddHelperMentorAsync(Ulid assignedWorkId, AddHelperMentorOptionsDTO options)
    {
        // Loaded whole rather than as one of the caller's own works: this is also staff's
        // call to make, and they are on nobody's work.
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        assignedWork.ThrowNotFoundIfNull();

        if (!_access.CanAssignHelperMentor(assignedWork))
        {
            throw new ForbiddenException();
        }

        if (
            assignedWork.MainMentorId == options.MentorId
            || assignedWork.HelperMentorId == options.MentorId
        )
        {
            return;
        }

        if (!await _userRepository.MentorExistsAsync(options.MentorId))
        {
            throw new NotFoundException();
        }

        assignedWork.HelperMentorId = options.MentorId;

        await _events.PublishAsync(
            new HelperMentorAddedEvent(
                assignedWork.Id,
                options.MentorId,
                _currentUser.RequireUserId()
            )
        );
    }

    public async Task ReplaceMainMentorAsync(
        Ulid assignedWorkId,
        ReplaceMainMentorOptionsDTO options
    )
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        assignedWork.ThrowNotFoundIfNull();

        if (!_access.CanAssignMainMentor(assignedWork))
        {
            throw new ForbiddenException();
        }

        if (
            assignedWork.MainMentorId == options.MentorId
            || assignedWork.HelperMentorId == options.MentorId
        )
        {
            return; // Mentor is already assigned to this work, nothing to replace
        }

        if (!await _userRepository.MentorExistsAsync(options.MentorId))
        {
            throw new NotFoundException();
        }

        var previousMainMentorId = assignedWork.MainMentorId;
        assignedWork.MainMentorId = options.MentorId;

        await _events.PublishAsync(
            new MainMentorChangedEvent(
                assignedWork.Id,
                options.MentorId,
                previousMainMentorId,
                _currentUser.RequireUserId()
            )
        );
    }
}
