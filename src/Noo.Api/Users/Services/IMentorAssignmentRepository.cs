using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

public interface IMentorAssignmentRepository : IRepository<MentorAssignmentModel>
{
    public Task<MentorAssignmentModel?> GetAsync(Ulid studentId, Ulid mentorId, Ulid? subjectId = null);
}
