using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Models;

namespace Noo.Api.Users.Services;

[RegisterScoped(typeof(IMentorAssignmentRepository))]
public class MentorAssignmentRepository : Repository<MentorAssignmentModel>, IMentorAssignmentRepository
{
    public MentorAssignmentRepository(NooDbContext context) : base(context)
    {
    }
    /// <summary>
	/// Gets mentor assignment by student, mentor and optional subject
    /// If subjectId is null, gets assignment regardless of subject
	/// </summary>
    public Task<MentorAssignmentModel?> GetAsync(Ulid studentId, Ulid mentorId, Ulid? subjectId = null)
    {
        return Context.GetDbSet<MentorAssignmentModel>()
            .FirstOrDefaultAsync(x =>
                x.StudentId == studentId &&
                x.MentorId == mentorId &&
                (subjectId == null || x.SubjectId == subjectId)
            );
    }
}
