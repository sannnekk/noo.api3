using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Users.Filters;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

public interface IMentorService
{
    public Task<SearchResult<MentorAssignmentModel>> GetMentorAssignmentsAsync(
        Ulid studentId,
        MentorAssignmentFilter filter
    );

    public Task<SearchResult<MentorAssignmentModel>> GetStudentAssignmentsAsync(
        Ulid mntorId,
        MentorAssignmentFilter filter
    );

    public Task<Ulid> AssignMentorAsync(Ulid studentId, Ulid mentorId, Ulid subjectId);

    public void UnassignMentor(Ulid assignmentId);
}
