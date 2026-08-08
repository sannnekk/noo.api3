using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Security.Authorization;
using Noo.Api.Core.System.Events;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Events;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;
using Noo.Api.Users.QuerySpecifications;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IMentorService))]
public class MentorService : IMentorService
{
    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;

    private readonly ICurrentUser _currentUser;

    private readonly IEventPublisher _events;

    public MentorService(
        IMentorAssignmentRepository mentorAssignmentRepository,
        ICurrentUser currentUser,
        IEventPublisher events
    )
    {
        _mentorAssignmentRepository = mentorAssignmentRepository;
        _currentUser = currentUser;
        _events = events;
    }

    public async Task<Ulid> AssignMentorAsync(Ulid studentId, Ulid mentorId, Ulid subjectId)
    {
        var existingAssignment = await _mentorAssignmentRepository.GetByStudentAndSubjectAsync(
            studentId,
            subjectId
        );

        if (existingAssignment != null && existingAssignment.MentorId != mentorId)
        {
            existingAssignment.MentorId = mentorId;
        }
        else if (existingAssignment == null)
        {
            existingAssignment = new MentorAssignmentModel
            {
                StudentId = studentId,
                MentorId = mentorId,
                SubjectId = subjectId,
            };

            _mentorAssignmentRepository.Add(existingAssignment);
        }

        await _events.PublishAsync(
            new MentorAssignedEvent(studentId, mentorId, subjectId, _currentUser.UserId)
        );

        return existingAssignment.Id;
    }

    public async Task UnassignMentorAsync(Ulid assignmentId)
    {
        var assignment = await _mentorAssignmentRepository.GetByIdAsync(assignmentId);

        if (assignment is null)
        {
            return;
        }

        _mentorAssignmentRepository.Delete(assignment);

        await _events.PublishAsync(
            new MentorUnassignedEvent(
                assignment.StudentId,
                assignment.MentorId,
                assignment.SubjectId,
                _currentUser.UserId
            )
        );
    }

    public Task<SearchResult<MentorAssignmentModel>> GetMentorAssignmentsAsync(
        Ulid studentId,
        MentorAssignmentFilter filter
    )
    {
        filter.StudentId = studentId;

        var specification = new StudentMentorAssignmentSpecification(studentId);

        return _mentorAssignmentRepository.SearchAsync(filter, [specification]);
    }

    public Task<SearchResult<MentorAssignmentModel>> GetStudentAssignmentsAsync(
        Ulid mentorId,
        MentorAssignmentFilter filter
    )
    {
        filter.MentorId = mentorId;

        var specification = new MentorAssignmentSpecification(mentorId);

        return _mentorAssignmentRepository.SearchAsync(filter, [specification]);
    }
}
