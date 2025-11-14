using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Exceptions;
using Noo.Api.AssignedWorks.Filters;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Services;
using MediatR;
using Noo.Api.AssignedWorks.Events;
using AutoMapper;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkService))]
public class AssignedWorkService : IAssignedWorkService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IAssignedWorkRepository _assignedWorkRepository;

    private readonly IAssignedWorkAnswerRepository _assignedWorkAnswerRepository;

    private readonly IAssignedWorkCommentRepository _assignedWorkCommentRepository;

    private readonly IUserRepository _userRepository;

    private readonly ICurrentUser _currentUser;

    private readonly IMediator _mediator;

    private readonly IMapper _mapper;

    public AssignedWorkService(IUnitOfWork unitOfWork, IUserRepository userRepository, ICurrentUser currentUser, IMediator mediator, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _assignedWorkRepository = _unitOfWork.AssignedWorkRepository();
        _assignedWorkAnswerRepository = _unitOfWork.AssignedWorkAnswerRepository();
        _assignedWorkCommentRepository = _unitOfWork.AssignedWorkCommentRepository();
        _userRepository = userRepository;
        _currentUser = currentUser;
        _mediator = mediator;
        _mapper = mapper;
    }

    public async Task AddHelperMentorAsync(Ulid assignedWorkId, AddHelperMentorOptionsDTO options)
    {
        var assignedWork = await _assignedWorkRepository.GetByIdAsync(assignedWorkId);

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (assignedWork.MainMentorId == options.MentorId || assignedWork.HelperMentorId == options.MentorId)
        {
            return; // Mentor is already the main mentor, no need to add as helper
        }

        var mentorExists = await _userRepository.MentorExistsAsync(options.MentorId);

        if (!mentorExists)
        {
            throw new NotFoundException();
        }

        if (assignedWork.IsChecked())
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        assignedWork.HelperMentorId = options.MentorId;

        await _mediator.Publish(new HelperMentorAddedEvent(AssignedWorkId: assignedWork.Id, HelperMentorId: options.MentorId));
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteAsync(Ulid assignedWorkId)
    {
        if (await _assignedWorkRepository.IsWorkSolveStatusAsync(assignedWorkId, AssignedWorkSolveStatus.NotSolved, AssignedWorkSolveStatus.InProgress))
        {
            _assignedWorkRepository.DeleteById(assignedWorkId);
            await _unitOfWork.CommitAsync();
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

    public Task<AssignedWorkProgressDTO?> GetProgressAsync(Ulid assignedWorkId)
    {
        return _assignedWorkRepository.GetProgressAsync(assignedWorkId, _currentUser.UserId);
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

        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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

        await _mediator.Publish(new AssignedWorkCheckedEvent(
            AssignedWorkId: assignedWork.Id,
            CheckedBy: _currentUser.UserId.Value
        ));
        await _unitOfWork.CommitAsync();
    }

    public async Task MarkAsSolvedAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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
        await _unitOfWork.CommitAsync();
    }

    public async Task<Ulid> RemakeAsync(Ulid assignedWorkId, RemakeAssignedWorkOptionsDTO options)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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
            newAssignedWork.ExcludedTaskIds = await _assignedWorkAnswerRepository.GetCorrectAnswerIdsAsync(assignedWorkId);
        }

        _assignedWorkRepository.Add(newAssignedWork);
        await _unitOfWork.CommitAsync();

        return newAssignedWork.Id;
    }

    public async Task ReplaceMainMentorAsync(Ulid assignedWorkId, ReplaceMainMentorOptionsDTO options)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (assignedWork.MainMentorId == options.MentorId || assignedWork.HelperMentorId == options.MentorId)
        {
            return; // Mentor is already the main mentor, no need to replace
        }

        if (assignedWork.IsChecked())
        {
            throw new AssignedWorkAlreadyCheckedException();
        }

        var OldMentorId = assignedWork.MainMentorId;
        assignedWork.MainMentorId = options.MentorId;

        await _mediator.Publish(new MainMentorChangedEvent(
            OldMentorId: OldMentorId,
            NewMentorId: options.MentorId,
            AssignedWorkId: assignedWork.Id,
            NotifyMentor: options.NotifyMentor,
            NotifyStudent: options.NotifyStudent
        ));
        await _unitOfWork.CommitAsync();
    }

    public async Task ReturnToCheckAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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

        await _mediator.Publish(new AssignedWorkReturnedToCheckEvent(AssignedWorkId: assignedWork.Id));
        await _unitOfWork.CommitAsync();
    }

    public async Task ReturnToSolveAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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

        await _mediator.Publish(new AssignedWorkReturnedToSolveEvent(AssignedWorkId: assignedWork.Id));
        await _unitOfWork.CommitAsync();
    }

    public async Task<Ulid> SaveAnswerAsync(Ulid assignedWorkId, UpsertAssignedWorkAnswerDTO dto)
    {
        var answer = _mapper.Map<AssignedWorkAnswerModel>(dto);
        _assignedWorkAnswerRepository.Add(answer);
        await _unitOfWork.CommitAsync();
        return answer.Id;
    }

    public async Task<Ulid> SaveCommentAsync(Ulid assignedWorkId, UpsertAssignedWorkCommentDTO comment)
    {
        var commentEntity = _mapper.Map<AssignedWorkCommentModel>(comment);
        _assignedWorkCommentRepository.Add(commentEntity);
        await _unitOfWork.CommitAsync();
        return commentEntity.Id;
    }

    public async Task ShiftDeadlineAsync(Ulid assignedWorkId, ShiftAssignedWorkDeadlineOptionsDTO options)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("Current user ID is not set.");
        }

        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

        if (assignedWork == null)
        {
            throw new NotFoundException();
        }

        if (_currentUser.UserRole == UserRoles.Student)
        {
            AssertCorrectStudentDeadlineShift(assignedWork, options.NewDeadline);
            assignedWork.SolveDeadlineAt = options.NewDeadline;
        }
        else if (_currentUser.UserRole == UserRoles.Mentor)
        {
            AssertCorrectMentorDeadlineShift(assignedWork, options.NewDeadline);
            assignedWork.CheckDeadlineAt = options.NewDeadline;
        }
        else
        {
            throw new ForbiddenException();
        }


        if (_currentUser.UserRole == UserRoles.Student)
        {
            await _mediator.Publish(new AssignedWorkSolveDeadlineShiftedEvent(
                AssignedWorkId: assignedWork.Id,
                NotifyOthers: options.NotifyOthers
            ));
        }
        else if (_currentUser.UserRole == UserRoles.Mentor)
        {
            await _mediator.Publish(new AssignedWorkCheckDeadlineShiftedEvent(
                AssignedWorkId: assignedWork.Id,
                ShiftedById: _currentUser.UserId.Value,
                NotifyOthers: options.NotifyOthers
            ));
        }

        await _unitOfWork.CommitAsync();
    }

    public async Task ArchiveAsync(Ulid assignedWorkId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("Current user ID is not set.");
        }

        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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

        await _unitOfWork.CommitAsync();
    }

    public async Task UnarchiveAsync(Ulid assignedWorkId)
    {
        var assignedWork = await _assignedWorkRepository.GetAsync(assignedWorkId, _currentUser.UserId);

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

        await _unitOfWork.CommitAsync();
    }

    private void AssertCorrectStudentDeadlineShift(AssignedWorkModel assignedWork, DateTime newDeadline)
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

    private void AssertCorrectMentorDeadlineShift(AssignedWorkModel assignedWork, DateTime newDeadline)
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
