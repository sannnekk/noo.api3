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
    public Task<List<AssignedWorkModel>> GetByWorkAssignmentAsync(
        Ulid workAssignmentId,
        Ulid userId
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

    /// <summary>
    /// Tracked load (for mutation) of the assigned work together with its answers,
    /// restricted to works the given user participates in as student or mentor.
    /// </summary>
    public Task<AssignedWorkModel?> GetWithAnswersAsync(Ulid assignedWorkId, Ulid? userId);

    /// <summary>
    /// Tracked load (for mutation) of the assigned work together with its answers
    /// and the work's tasks. Used by the solve flow to run the automatic check.
    /// </summary>
    public Task<AssignedWorkModel?> GetWithAnswersAndTasksAsync(Ulid assignedWorkId);
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

    /// <summary>
    /// Returns the assigned-work counts (total, not solved, not checked, checked)
    /// for the given user in a single aggregate query. The user is matched as
    /// student, main mentor or helper mentor.
    /// </summary>
    public Task<AssignedWorksCounts> GetCountsForUserAsync(Ulid userId);
}
