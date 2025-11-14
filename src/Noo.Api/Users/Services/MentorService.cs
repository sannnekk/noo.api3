using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IMentorService))]
public class MentorService : IMentorService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IMentorAssignmentRepository _mentorAssignmentRepository;

    public MentorService(IUnitOfWork unitOfWork, IMentorAssignmentRepository mentorAssignmentRepository)
    {
        _unitOfWork = unitOfWork;
        _mentorAssignmentRepository = mentorAssignmentRepository;
    }

    public async Task<Ulid> AssignMentorAsync(Ulid studentId, Ulid mentorId, Ulid subjectId)
    {
        var existingAssignment = await _mentorAssignmentRepository.GetAsync(studentId, mentorId, subjectId);

        if (existingAssignment == null)
        {
            existingAssignment = new MentorAssignmentModel
            {
                StudentId = studentId,
                MentorId = mentorId,
                SubjectId = subjectId
            };

            _mentorAssignmentRepository.Add(existingAssignment);
            await _unitOfWork.CommitAsync();
        }

        return existingAssignment.Id;
    }

    public Task UnassignMentorAsync(Ulid assignmentId)
    {
        _mentorAssignmentRepository.DeleteById(assignmentId);
        return _unitOfWork.CommitAsync();
    }

    public Task<SearchResult<MentorAssignmentModel>> GetMentorAssignmentsAsync(Ulid studentId, MentorAssignmentFilter filter)
    {
        filter.StudentId = studentId;

        return _mentorAssignmentRepository.SearchAsync(filter);
    }

    public Task<SearchResult<MentorAssignmentModel>> GetStudentAssignmentsAsync(Ulid mentorId, MentorAssignmentFilter filter)
    {
        filter.MentorId = mentorId;

        return _mentorAssignmentRepository.SearchAsync(filter);
    }
}
