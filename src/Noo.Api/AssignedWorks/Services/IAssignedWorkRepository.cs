using System.Linq.Expressions;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Users.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.AssignedWorks.Services;

public interface IAssignedWorkRepository : IRepository<AssignedWorkModel>
{
    /// <summary>
    /// Returns array because there can be multiple AssignedWork's as separate attempts for one WorkAssignment.
    /// </summary>
    public Task<List<AssignedWorkProgressDTO>> GetProgressAsync(
        Ulid workAssignmentId,
        Ulid? userId
    );
    public Task<int> GetCurrentAttemptAsync(Ulid workAssignmentId, Ulid userId);
    public Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId);
    public Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId, Ulid? userId);
    public Task<bool> IsMentorOwnWorkAsync(Ulid assignedWorkId, Ulid userId);
    public Task<bool> IsStudentOwnWorkAsync(Ulid assignedWorkId, Ulid userId);
    public Task<bool> IsWorkCheckStatusAsync(
        Ulid assignedWorkId,
        params AssignedWorkCheckStatus[] statuses
    );
    public Task<bool> IsWorkSolveStatusAsync(
        Ulid assignedWorkId,
        params AssignedWorkSolveStatus[] statuses
    );
    public Task<AssignedWorkModel?> GetWholeAsync(Ulid assignedWorkId);
    public Task<AssignedWorkModel?> GetWithStudentAsync(Ulid assignedWorkId);
    public Task<int> GetCountAsync(
        Expression<Func<AssignedWorkModel, bool>> predicate,
        DateTime from,
        DateTime to
    );
    public Task<List<UserModel>> GetUsersByWorkIdAsync(Ulid workId);
    public Task<Dictionary<DateTime, int>> GetByDateRangeAsync(
        Expression<Func<AssignedWorkModel, bool>> predicate,
        DateTime from,
        DateTime to
    );
    public Task<Dictionary<DateTime, double?>> GetMonthAverageScoresAsync(
        Ulid studentId,
        WorkType? workType
    );
}
