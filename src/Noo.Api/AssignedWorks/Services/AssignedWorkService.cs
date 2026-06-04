using AutoMapper;
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
using Noo.Api.Core.Utils;
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
    private readonly IAssignedWorkCommentRepository _assignedWorkCommentRepository;
    private readonly ICourseWorkAssignmentRepository _workAssignmentRepository;
    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITaskCheckService _taskCheckService;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _events;
    private readonly ICacheRepository _cache;

    public AssignedWorkService(
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkAnswerRepository assignedWorkAnswerRepository,
        IAssignedWorkCommentRepository assignedWorkCommentRepository,
        ICourseWorkAssignmentRepository workAssignmentRepository,
        IMentorAssignmentRepository mentorAssignmentRepository,
        IUserRepository userRepository,
        ITaskCheckService taskCheckService,
        ICurrentUser currentUser,
        IMapper mapper,
        IEventPublisher events,
        ICacheRepository cache
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _assignedWorkAnswerRepository = assignedWorkAnswerRepository;
        _assignedWorkCommentRepository = assignedWorkCommentRepository;
        _workAssignmentRepository = workAssignmentRepository;
        _mentorAssignmentRepository = mentorAssignmentRepository;
        _userRepository = userRepository;
        _taskCheckService = taskCheckService;
        _currentUser = currentUser;
        _mapper = mapper;
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

    public async Task AddHelperMentorAsync(Ulid assignedWorkId, AddHelperMentorOptionsDTO options)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        assignedWork.ThrowNotFoundIfNull();

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

        if (assignedWork.IsChecked)
        {
            throw new AssignedWorkAlreadyCheckedException();
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

    public async Task DeleteAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            return;
        }

        if (assignedWork.CanBeDeleted)
        {
            _assignedWorkRepository.DeleteById(assignedWorkId);
        }
    }

    public async Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetWholeAsync(assignedWorkId);

        if (assignedWork == null)
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

        return assignedWork;
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

    public Task<List<AssignedWorkModel>> GetByWorkAssignmentAsync(Ulid workAssignmentId)
    {
        var userId = _currentUser.RequireUserId();

        return _assignedWorkRepository.GetByWorkAssignmentAsync(workAssignmentId, userId);
    }

    public Task<SearchResult<AssignedWorkModel>> GetAssignedWorksAsync(AssignedWorkFilter filter)
    {
        var role = _currentUser.RequireUserRole();

        var specification = new AssignedWorkSearchSpecification(role);
        return _assignedWorkRepository.SearchAsync(filter, [specification]);
    }

    public async Task MarkAsCheckedAsync(Ulid assignedWorkId)
    {
        var userId = _currentUser.RequireUserId();

        var assignedWork = await _assignedWorkRepository.GetWithAnswersAsync(
            assignedWorkId,
            userId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsSolved)
        {
            throw new AssignedWorkNotSolvedException();
        }

        if (assignedWork.IsChecked)
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        foreach (var answer in assignedWork.Answers)
        {
            answer.Status = AssignedWorkAnswerStatus.Checked;
        }

        assignedWork.CheckedAt = Clock.Now;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.Checked;

        await _events.PublishAsync(new CheckedEvent(assignedWork.Id, userId));
    }

    public async Task MarkAsSolvedAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetWithAnswersAndTasksAsync(
            assignedWorkId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (_currentUser.UserId != assignedWork.StudentId)
        {
            throw new NotFoundException();
        }

        if (assignedWork.IsSolved)
        {
            throw new AssignedWorkAlreadySolvedException();
        }

        foreach (var answer in assignedWork.Answers)
        {
            answer.Status = AssignedWorkAnswerStatus.Submitted;
        }

        var score = _taskCheckService.CheckTasks(
            assignedWork.Answers,
            assignedWork.Work?.Tasks ?? []
        );

        assignedWork.Score = score;
        assignedWork.SolvedAt = Clock.Now;
        assignedWork.SolveStatus = AssignedWorkSolveStatus.Solved;

        await _events.PublishAsync(new SolvedEvent(assignedWork.Id, assignedWork.StudentId));
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
                await _assignedWorkAnswerRepository.GetCorrectAnswerIdsAsync(assignedWorkId);
        }

        _assignedWorkRepository.Add(newAssignedWork);

        return newAssignedWork.Id;
    }

    public async Task ReplaceMainMentorAsync(
        Ulid assignedWorkId,
        ReplaceMainMentorOptionsDTO options
    )
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (
            assignedWork.MainMentorId == options.MentorId
            || assignedWork.HelperMentorId == options.MentorId
        )
        {
            return; // Mentor is already assigned to this work, nothing to replace
        }

        if (assignedWork.IsChecked)
        {
            throw new AssignedWorkAlreadyCheckedException();
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

    public async Task ReturnToCheckAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsChecked)
        {
            throw new AssignedWorkNotCheckedException();
        }

        assignedWork.CheckedAt = null;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.NotChecked;

        await _events.PublishAsync(
            new SentOnRecheckEvent(assignedWork.Id, _currentUser.RequireUserId())
        );
    }

    public async Task ReturnToSolveAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        if (!assignedWork.IsSolved)
        {
            throw new AssignedWorkNotSolvedException();
        }

        assignedWork.CheckedAt = null;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.NotChecked;
        assignedWork.Score = null;

        assignedWork.SolvedAt = null;
        assignedWork.SolveStatus = AssignedWorkSolveStatus.InProgress;

        await _events.PublishAsync(
            new SentOnResolveEvent(assignedWork.Id, _currentUser.RequireUserId())
        );
    }

    public async Task<Ulid> SaveAnswerAsync(Ulid assignedWorkId, UpsertAssignedWorkAnswerDTO dto)
    {
        Ulid answerId;

        // It's an update to an existing answer (e.g. a student editing or a mentor commenting).
        if (dto.Id.HasValue)
        {
            var existing = await _assignedWorkAnswerRepository.GetByIdAsync(dto.Id.Value);

            existing.ThrowNotFoundIfNull();

            if (existing.AssignedWorkId != assignedWorkId)
            {
                throw new NotFoundException();
            }

            _mapper.Map(dto, existing);
            answerId = existing.Id;
        }
        else
        {
            var answer = _mapper.Map<AssignedWorkAnswerModel>(dto);

            answer.AssignedWorkId = assignedWorkId;
            _assignedWorkAnswerRepository.Add(answer);
            answerId = answer.Id;
        }

        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        // Saving an answer is the implicit "started" signal: a student saving their answer starts
        // solving, a mentor saving a comment/score starts checking. The event fires only on the
        // first transition out of the not-started state.
        switch (_currentUser.UserRole)
        {
            case UserRoles.Student:
                if (assignedWork.SolveStatus == AssignedWorkSolveStatus.NotSolved)
                {
                    assignedWork.SolveStatus = AssignedWorkSolveStatus.InProgress;
                    await _events.PublishAsync(
                        new StartedSolvingEvent(assignedWork.Id, assignedWork.StudentId)
                    );
                }
                break;
            case UserRoles.Mentor:
                if (assignedWork.CheckStatus == AssignedWorkCheckStatus.NotChecked)
                {
                    assignedWork.CheckStatus = AssignedWorkCheckStatus.InProgress;
                    await _events.PublishAsync(
                        new StartedCheckingEvent(assignedWork.Id, _currentUser.RequireUserId())
                    );
                }
                break;
        }

        return answerId;
    }

    public Ulid SaveComment(Ulid assignedWorkId, UpsertAssignedWorkCommentDTO comment)
    {
        var commentEntity = _mapper.Map<AssignedWorkCommentModel>(comment);
        _assignedWorkCommentRepository.Add(commentEntity);
        return commentEntity.Id;
    }

    public async Task ShiftDeadlineAsync(
        Ulid assignedWorkId,
        ShiftAssignedWorkDeadlineOptionsDTO options
    )
    {
        var userId = _currentUser.RequireUserId();

        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, userId);

        assignedWork.ThrowNotFoundIfNull();

        switch (_currentUser.UserRole)
        {
            case UserRoles.Student:
                AssertCorrectStudentDeadlineShift(assignedWork, options.NewDeadline);
                assignedWork.SolveDeadlineAt = options.NewDeadline;
                break;
            case UserRoles.Mentor:
                AssertCorrectMentorDeadlineShift(assignedWork, options.NewDeadline);
                assignedWork.CheckDeadlineAt = options.NewDeadline;
                break;
            default:
                throw new ForbiddenException();
        }

        await _events.PublishAsync(
            new DeadlineShiftedEvent(
                assignedWork.Id,
                new ShiftDeadlinePayload
                {
                    NewDeadlineAt = options.NewDeadline,
                    ShiftedByRole = _currentUser.RequireUserRole(),
                    ShiftedById = userId,
                }
            )
        );
    }

    public async Task ArchiveAsync(Ulid assignedWorkId)
    {
        var userId = _currentUser.RequireUserId();

        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, userId);

        assignedWork.ThrowNotFoundIfNull();

        switch (_currentUser.UserRole)
        {
            case UserRoles.Student:
                assignedWork.IsArchivedByStudent = true;
                break;
            case UserRoles.Mentor:
                assignedWork.IsArchivedByMentors = true;
                break;
            case UserRoles.Admin:
            case UserRoles.Assistant:
            case UserRoles.Teacher:
                assignedWork.IsArchivedByAssistants = true;
                break;
            default:
                throw new ForbiddenException();
        }
    }

    public async Task UnarchiveAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        assignedWork.ThrowNotFoundIfNull();

        switch (_currentUser.UserRole)
        {
            case UserRoles.Student:
                assignedWork.IsArchivedByStudent = false;
                break;
            case UserRoles.Mentor:
                assignedWork.IsArchivedByMentors = false;
                break;
            case UserRoles.Admin:
            case UserRoles.Assistant:
            case UserRoles.Teacher:
                assignedWork.IsArchivedByAssistants = false;
                break;
            default:
                throw new ForbiddenException();
        }
    }

    private void AssertCorrectStudentDeadlineShift(
        AssignedWorkModel assignedWork,
        DateTime newDeadline
    )
    {
        if (newDeadline - assignedWork.SolveDeadlineAt > AssignedWorkConfig.MaxSolveDeadlineShift)
        {
            throw new IncorrectDeadlineShiftException();
        }

        if (assignedWork.IsSolved)
        {
            throw new AssignedWorkAlreadySolvedException();
        }
    }

    private void AssertCorrectMentorDeadlineShift(
        AssignedWorkModel assignedWork,
        DateTime newDeadline
    )
    {
        if (newDeadline - assignedWork.CheckDeadlineAt > AssignedWorkConfig.MaxCheckDeadlineShift)
        {
            throw new IncorrectDeadlineShiftException();
        }

        if (assignedWork.IsChecked)
        {
            throw new AssignedWorkAlreadyCheckedException();
        }
    }
}
