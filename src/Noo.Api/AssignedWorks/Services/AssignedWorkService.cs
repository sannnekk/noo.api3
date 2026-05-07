using AutoMapper;
using MediatR;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Courses.Services;
using Noo.Api.Users.Services;
using Noo.Api.Works.Services;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkService))]
public class AssignedWorkService : IAssignedWorkService
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkAnswerRepository _assignedWorkAnswerRepository;
    private readonly IAssignedWorkCommentRepository _assignedWorkCommentRepository;
    private readonly ICourseWorkAssignmentRepository _workAssignmentRepository;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public AssignedWorkService(
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkAnswerRepository assignedWorkAnswerRepository,
        IAssignedWorkCommentRepository assignedWorkCommentRepository,
        ICourseWorkAssignmentRepository workAssignmentRepository,
        IMentorAssignmentRepository mentorAssignmentRepository,
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        IMediator mediator,
        IMapper mapper
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _assignedWorkAnswerRepository = assignedWorkAnswerRepository;
        _assignedWorkCommentRepository = assignedWorkCommentRepository;
        _workAssignmentRepository = workAssignmentRepository;
        _mentorAssignmentRepository = mentorAssignmentRepository;
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task<Ulid> CreateAsync(Ulid workAssignmentId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("Current user ID is not set.");
        }

        var workAssignment = await _workAssignmentRepository.GetWithWorkAsync(workAssignmentId);

        workAssignment.ThrowNotFoundIfNull();
        workAssignment.Work.ThrowNotFoundIfNull();
        workAssignment.Work.SubjectId.ThrowNotFoundIfNull();

        var mentor = await _mentorAssignmentRepository.GetMentorAsync(
            _currentUser.UserId.Value,
            workAssignment.Work.SubjectId.Value
        );

        mentor.ThrowNotFoundIfNull();

        var attemptCount = await _assignedWorkRepository.GetCurrentAttemptAsync(
            workAssignmentId,
            _currentUser.UserId.Value
        );

        var maxScore = await _workTaskRepository.GetWorkMaxScoreAsync(workAssignment.WorkId);

        var newAssignedWork = AssignedWorkModel.CreateNew(
            workAssignment,
            _currentUser.UserId.Value,
            maxScore,
            mentor.Id,
            attemptCount + 1
        );

        _assignedWorkRepository.Add(newAssignedWork);

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
            return; // Mentor is already the main mentor, no need to add as helper
        }

        // TODO: check if mentor exists

        if (assignedWork.IsChecked())
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        assignedWork.HelperMentorId = options.MentorId;

        await _mediator.Publish(
            new HelperMentorAddedEvent(
                AssignedWorkId: assignedWork.Id,
                HelperMentorId: options.MentorId
            )
        );
    }

    public async Task DeleteAsync(Ulid assignedWorkId)
    {
        // TODO: refactor -> aw.CanBeDeleted()
        if (
            await _assignedWorkRepository.IsWorkSolveStatusAsync(
                assignedWorkId,
                AssignedWorkSolveStatus.NotSolved,
                AssignedWorkSolveStatus.InProgress
            )
        )
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
            if (answer.Status == AssignedWorkAnswerStatus.Checked)
            {
                answer.MentorComment = null;
                answer.Score = null;
                answer.DetailedScore = null;
            }
        }

        return assignedWork;
    }

    public Task<List<AssignedWorkProgressDTO>> GetProgressAsync(Ulid workAssignmentId)
    {
        return _assignedWorkRepository.GetProgressAsync(workAssignmentId, _currentUser.UserId);
    }

    public Task<SearchResult<AssignedWorkModel>> GetAssignedWorksAsync(AssignedWorkFilter filter)
    {
        return _assignedWorkRepository.SearchAsync(filter);
    }

    public async Task MarkAsCheckedAsync(Ulid assignedWorkId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("Current user ID is not set.");
        }

        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (!assignedWork.IsSolved())
        {
            throw new AssignedWorkNotSolvedException();
        }

        if (assignedWork.IsChecked())
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        assignedWork.CheckedAt = DateTime.UtcNow;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.Checked;

        await _mediator.Publish(
            new AssignedWorkCheckedEvent(
                AssignedWorkId: assignedWork.Id,
                CheckedBy: _currentUser.UserId.Value
            )
        );
    }

    public async Task MarkAsSolvedAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (assignedWork.IsSolved())
        {
            throw new AssignedWorkAlreadySolvedException();
        }

        assignedWork.SolvedAt = DateTime.UtcNow;
        assignedWork.SolveStatus = AssignedWorkSolveStatus.Solved;

        await _mediator.Publish(new AssignedWorkSolvedEvent(AssignedWorkId: assignedWork.Id));
    }

    public async Task<Ulid> RemakeAsync(Ulid assignedWorkId, RemakeAssignedWorkOptionsDTO options)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (!assignedWork.IsRemakeable())
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

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (
            assignedWork.MainMentorId == options.MentorId
            || assignedWork.HelperMentorId == options.MentorId
        )
        {
            return; // Mentor is already the main mentor, no need to replace
        }

        if (assignedWork.IsChecked())
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        var OldMentorId = assignedWork.MainMentorId;
        assignedWork.MainMentorId = options.MentorId;

        await _mediator.Publish(
            new MainMentorChangedEvent(
                OldMentorId: OldMentorId,
                NewMentorId: options.MentorId,
                AssignedWorkId: assignedWork.Id,
                NotifyMentor: options.NotifyMentor,
                NotifyStudent: options.NotifyStudent
            )
        );
    }

    public async Task ReturnToCheckAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (!assignedWork.IsChecked())
        {
            throw new AssignedWorkNotCheckedException();
        }

        assignedWork.CheckedAt = null;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.NotChecked;

        await _mediator.Publish(
            new AssignedWorkReturnedToCheckEvent(AssignedWorkId: assignedWork.Id)
        );
    }

    public async Task ReturnToSolveAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (!assignedWork.IsSolved())
        {
            throw new AssignedWorkNotSolvedException();
        }

        assignedWork.CheckedAt = null;
        assignedWork.CheckStatus = AssignedWorkCheckStatus.NotChecked;
        assignedWork.Score = null;

        assignedWork.SolvedAt = null;
        assignedWork.SolveStatus = AssignedWorkSolveStatus.InProgress;

        await _mediator.Publish(
            new AssignedWorkReturnedToSolveEvent(AssignedWorkId: assignedWork.Id)
        );
    }

    public Ulid SaveAnswer(Ulid assignedWorkId, UpsertAssignedWorkAnswerDTO dto)
    {
        // TODO: refactor
        var answer = _mapper.Map<AssignedWorkAnswerModel>(dto);
        _assignedWorkAnswerRepository.Add(answer);
        return answer.Id;
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
        if (!_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("Current user ID is not set.");
        }

        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (_currentUser.UserRole == UserRoles.Student)
        {
            AssertCorrectStudentDeadlineShift(assignedWork, options.NewDeadline);
            assignedWork.SolveDeadlineAt = options.NewDeadline;
            await _mediator.Publish(
                new AssignedWorkSolveDeadlineShiftedEvent(
                    AssignedWorkId: assignedWork.Id,
                    NotifyOthers: options.NotifyOthers
                )
            );
        }
        else if (_currentUser.UserRole == UserRoles.Mentor)
        {
            AssertCorrectMentorDeadlineShift(assignedWork, options.NewDeadline);
            assignedWork.CheckDeadlineAt = options.NewDeadline;

            await _mediator.Publish(
                new AssignedWorkCheckDeadlineShiftedEvent(
                    AssignedWorkId: assignedWork.Id,
                    ShiftedById: _currentUser.UserId.Value,
                    NotifyOthers: options.NotifyOthers
                )
            );
        }
    }

    public async Task ArchiveAsync(Ulid assignedWorkId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("Current user ID is not set.");
        }

        var assignedWork = await _assignedWorkRepository.GetAsync(
            assignedWorkId,
            _currentUser.UserId
        );

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

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

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

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

        if (assignedWork.IsSolved())
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

        if (assignedWork.IsChecked())
        {
            throw new AssignedWorkAlreadyCheckedException();
        }
    }
}
