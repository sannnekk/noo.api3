using AutoMapper;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Events;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkEditingService))]
public class AssignedWorkEditingService : IAssignedWorkEditingService
{
    private readonly IAssignedWorkRepository _assignedWorkRepository;
    private readonly IAssignedWorkAnswerRepository _assignedWorkAnswerRepository;
    private readonly IAssignedWorkCommentRepository _assignedWorkCommentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IEventPublisher _events;

    public AssignedWorkEditingService(
        IAssignedWorkRepository assignedWorkRepository,
        IAssignedWorkAnswerRepository assignedWorkAnswerRepository,
        IAssignedWorkCommentRepository assignedWorkCommentRepository,
        ICurrentUser currentUser,
        IMapper mapper,
        IEventPublisher events
    )
    {
        _assignedWorkRepository = assignedWorkRepository;
        _assignedWorkAnswerRepository = assignedWorkAnswerRepository;
        _assignedWorkCommentRepository = assignedWorkCommentRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _events = events;
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

        await MarkAsStartedAsync(assignedWork);

        return answerId;
    }

    public async Task<Ulid> SaveCommentAsync(Ulid assignedWorkId, UpsertAssignedWorkCommentDTO dto)
    {
        var userId = _currentUser.RequireUserId();

        var assignedWork = await _assignedWorkRepository.GetWithCommentsAsync(
            assignedWorkId,
            userId
        );

        assignedWork.ThrowNotFoundIfNull();

        // Everyone writing on a work has exactly one comment on it, and which one is decided
        // by the seat they hold on that work — never by the id the client sent.
        var seat = SeatOf(assignedWork, userId);

        var comment = seat switch
        {
            CommentSeat.Student => assignedWork.StudentComment,
            CommentSeat.MainMentor => assignedWork.MainMentorComment,
            CommentSeat.HelperMentor => assignedWork.HelperMentorComment,
            _ => throw new ForbiddenException(),
        };

        if (comment == null)
        {
            comment = _mapper.Map<AssignedWorkCommentModel>(dto);

            _assignedWorkCommentRepository.Add(comment);

            switch (seat)
            {
                case CommentSeat.Student:
                    assignedWork.StudentComment = comment;
                    break;
                case CommentSeat.MainMentor:
                    assignedWork.MainMentorComment = comment;
                    break;
                case CommentSeat.HelperMentor:
                    assignedWork.HelperMentorComment = comment;
                    break;
            }
        }
        else
        {
            comment.Content = dto.Content;
        }

        await MarkAsStartedAsync(assignedWork);

        return comment.Id;
    }

    /// <summary>
    /// Which of the three comments on a work belongs to the given user.
    /// </summary>
    private enum CommentSeat
    {
        Student,
        MainMentor,
        HelperMentor,
    }

    private static CommentSeat SeatOf(AssignedWorkModel assignedWork, Ulid userId)
    {
        if (assignedWork.StudentId == userId)
        {
            return CommentSeat.Student;
        }

        if (assignedWork.MainMentorId == userId)
        {
            return CommentSeat.MainMentor;
        }

        if (assignedWork.HelperMentorId == userId)
        {
            return CommentSeat.HelperMentor;
        }

        throw new ForbiddenException();
    }

    /// <summary>
    /// Writing on a work is the implicit "started" signal: a student saving an answer or a
    /// comment starts solving, a mentor saving one starts checking. The event fires only on
    /// the first transition out of the not-started state.
    /// </summary>
    private async Task MarkAsStartedAsync(AssignedWorkModel assignedWork)
    {
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
    }
}
