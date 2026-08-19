using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Specifications;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Cache;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;
using Noo.Api.Users.Models;
using Noo.Api.Users.Services;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkService))]
public class AssignedWorkService : IAssignedWorkService
{
    private static readonly TimeSpan _metadataCacheTtl = TimeSpan.FromSeconds(30);

    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkAnswerRepository _assignedWorkAnswerRepository;
    private readonly ICourseWorkAssignmentRepository _workAssignmentRepository;
    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;
    private readonly IAssignedWorkAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IEventPublisher _events;
    private readonly ICacheRepository _cache;

    public AssignedWorkService(
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkAnswerRepository assignedWorkAnswerRepository,
        ICourseWorkAssignmentRepository workAssignmentRepository,
        IMentorAssignmentRepository mentorAssignmentRepository,
        IAssignedWorkAccessService access,
        ICurrentUser currentUser,
        IEventPublisher events,
        ICacheRepository cache
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _assignedWorkAnswerRepository = assignedWorkAnswerRepository;
        _workAssignmentRepository = workAssignmentRepository;
        _mentorAssignmentRepository = mentorAssignmentRepository;
        _access = access;
        _currentUser = currentUser;
        _events = events;
        _cache = cache;
    }

    private static string MetadataCacheKey(Ulid userId) => $"assigned-work:metadata:{userId}";

    public async Task<Ulid> CreateAsync(Ulid workAssignmentId)
    {
        var userId = _currentUser.RequireUserId();

        var workAssignment = await _workAssignmentRepository.GetWithWorkAsync(workAssignmentId);

        workAssignment.ThrowNotFoundIfNull();
        workAssignment.Work.ThrowNotFoundIfNull();
        workAssignment.Work.SubjectId.ThrowNotFoundIfNull();

        UserModel? mentor = null;

        if (workAssignment.Work.NeedsMentor)
        {
            mentor = await _mentorAssignmentRepository.GetMentorAsync(
                userId,
                workAssignment.Work.SubjectId.Value
            );

            mentor.ThrowNotFoundIfNull();
        }

        var attemptCount = await _assignedWorkRepository.GetCurrentAttemptAsync(
            workAssignmentId,
            userId
        );

        var newAssignedWork = AssignedWorkModel.CreateNew(
            workAssignment,
            userId,
            workAssignment.Work.MaxScore,
            mentor?.Id,
            attemptCount + 1
        );

        _assignedWorkRepository.Add(newAssignedWork);

        await _events.PublishAsync(new CreatedEvent(newAssignedWork.Id));

        return newAssignedWork.Id;
    }

    public async Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetWholeAsync(assignedWorkId);

        if (assignedWork == null)
        {
            return null;
        }

        // A work carries the student's answers, so who is allowed to open it is not something
        // the role gate alone can settle. Reads by a bystander look like a work that isn't
        // there, rather than one they are being kept out of.
        if (!_access.CanRead(assignedWork))
        {
            return null;
        }

        foreach (var answer in assignedWork.Answers)
        {
            if (answer.Status == AssignedWorkAnswerStatus.NotSubmitted)
            {
                answer.MentorComment = null;
                answer.Score = null;
                answer.DetailedScore = null;
            }
        }

        // A mentor's comment on the work is part of the check result: the student gets it
        // once the work has been checked, not while it is still being written. Safe to strip
        // in place only because the work is loaded untracked.
        if (
            _currentUser.UserRole == UserRoles.Student
            && !AssignedWorkStatuses.Checked.Contains(assignedWork.CheckStatus)
        )
        {
            assignedWork.MainMentorComment = null;
            assignedWork.MainMentorCommentId = null;
            assignedWork.HelperMentorComment = null;
            assignedWork.HelperMentorCommentId = null;

            // The answer key does not travel with the work a student is still solving.
            // Tasks that offer their answer up front hand it over one at a time, through
            // IAssignedWorkTaskService, and a task checked on its own is checked by the
            // server — neither works if the key is sitting in the page already.
            foreach (var task in assignedWork.Work?.Tasks ?? [])
            {
                task.RightAnswers = null;
            }
        }

        return assignedWork;
    }

    public Task<List<AssignedWorkModel>> GetByWorkAssignmentAsync(Ulid workAssignmentId)
    {
        var userId = _currentUser.RequireUserId();

        return _assignedWorkRepository.GetByWorkAssignmentAsync(workAssignmentId, userId);
    }

    public Task<SearchResult<AssignedWorkModel>> GetAssignedWorksAsync(AssignedWorkFilter filter)
    {
        var role = _currentUser.RequireUserRole();

        var specification = new AssignedWorkSearchSpecification(role, filter.Tab);
        return _assignedWorkRepository.SearchAsync(filter, [specification]);
    }

    public async Task<AssignedWorksMetadataDTO> GetMetadataAsync(Ulid userId)
    {
        var counts = await _cache.GetOrSetAsync(
            MetadataCacheKey(userId),
            () => _assignedWorkRepository.GetCountsForUserAsync(userId),
            _metadataCacheTtl
        );

        return new AssignedWorksMetadataDTO { Counts = counts ?? new AssignedWorksCounts() };
    }

    public async Task<Ulid> RemakeAsync(Ulid assignedWorkId, RemakeAssignedWorkOptionsDTO options)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsRemakeable)
        {
            throw new AssignedWorkNotRemakeableException();
        }

        var newAssignedWork = assignedWork.NewAttemptCopy();

        if (options.IncludeOnlyWrongTasks)
        {
            newAssignedWork.ExcludedTaskIds =
                await _assignedWorkAnswerRepository.GetCorrectlyAnsweredTaskIdsAsync(
                    assignedWorkId
                );
        }

        _assignedWorkRepository.Add(newAssignedWork);

        return newAssignedWork.Id;
    }

    public Task ArchiveAsync(Ulid assignedWorkId) => SetArchivedAsync(assignedWorkId, true);

    public Task UnarchiveAsync(Ulid assignedWorkId) => SetArchivedAsync(assignedWorkId, false);

    public async Task DeleteAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        if (assignedWork == null)
        {
            return;
        }

        if (!_access.CanDelete(assignedWork))
        {
            throw new AssignedWorkAlreadySolvedException();
        }

        _assignedWorkRepository.DeleteById(assignedWorkId);
    }

    /// <summary>
    /// Each side of a work archives it out of their own list only: a mentor tidying theirs
    /// away leaves the student's list alone, and vice versa.
    /// </summary>
    private async Task SetArchivedAsync(Ulid assignedWorkId, bool isArchived)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        assignedWork.ThrowNotFoundIfNull();

        if (!_access.CanArchive(assignedWork))
        {
            throw new ForbiddenException();
        }

        switch (_currentUser.UserRole)
        {
            case UserRoles.Student:
                assignedWork.IsArchivedByStudent = isArchived;
                break;
            case UserRoles.Mentor:
                assignedWork.IsArchivedByMentors = isArchived;
                break;
            case UserRoles.Admin:
            case UserRoles.Assistant:
            case UserRoles.Teacher:
                assignedWork.IsArchivedByAssistants = isArchived;
                break;
            default:
                throw new ForbiddenException();
        }
    }
}
