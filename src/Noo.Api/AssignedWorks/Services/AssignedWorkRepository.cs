using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Specifications;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkRepository))]
public class AssignedWorkRepository : Repository<AssignedWorkModel>, IAssignedWorkRepository
{
    public AssignedWorkRepository(NooDbContext dbContext)
        : base(dbContext) { }

    /// <summary>
    /// Nothing at all: an anonymous caller takes part in no work.
    /// </summary>
    private static Task<AssignedWorkModel?> NoWork => Task.FromResult<AssignedWorkModel?>(null);

    /// <summary>
    /// The works the user takes part in, or <c>null</c> when there is no user to ask about.
    /// </summary>
    private IQueryable<AssignedWorkModel>? ParticipatedBy(Ulid? userId)
    {
        return userId == null
            ? null
            : Context
                .Set<AssignedWorkModel>()
                .Where(AssignedWorkCriteria.ParticipatedBy(userId.Value));
    }

    public Task<List<AssignedWorkModel>> GetByWorkAssignmentAsync(
        Ulid workAssignmentId,
        Ulid userId
    )
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(aw => aw.CourseWorkAssignmentId == workAssignmentId && aw.StudentId == userId)
            .ToListAsync();
    }

    public Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId, Ulid? userId)
    {
        return ParticipatedBy(userId)
            ?.Where(aw => aw.Id == assignedWorkId)
            .FirstOrDefaultAsync() ?? NoWork;
    }

    public Task<AssignedWorkModel?> GetWholeAsync(Ulid assignedWorkId)
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.Answers)
            .Include(aw => aw.Student)
            .Include(aw => aw.MainMentor)
            .Include(aw => aw.HelperMentor)
            .Include(aw => aw.Work)
                .ThenInclude(w => w!.Tasks)
            .Include(aw => aw.StudentComment)
            .Include(aw => aw.MainMentorComment)
            .Include(aw => aw.HelperMentorComment)
            .AsSplitQuery() //! To avoid Cartesian product issues, DO NOT REMOVE
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public Task<AssignedWorkModel?> GetWithCommentsAsync(Ulid assignedWorkId, Ulid? userId)
    {
        return ParticipatedBy(userId)
            ?.Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.StudentComment)
            .Include(aw => aw.MainMentorComment)
            .Include(aw => aw.HelperMentorComment)
            .FirstOrDefaultAsync() ?? NoWork;
    }

    public Task<AssignedWorkModel?> GetWithAnswersAsync(Ulid assignedWorkId, Ulid? userId)
    {
        return ParticipatedBy(userId)
            ?.Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.Answers)
            .FirstOrDefaultAsync() ?? NoWork;
    }

    public Task<AssignedWorkModel?> GetWithAnswersAndTasksAsync(Ulid assignedWorkId)
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.Answers)
            .Include(aw => aw.Work)
                .ThenInclude(w => w!.Tasks)
            .AsSplitQuery() //! To avoid Cartesian product issues, DO NOT REMOVE
            .FirstOrDefaultAsync();
    }

    public Task<int> GetCountAsync(
        Expression<Func<AssignedWorkModel, bool>> predicate,
        DateTime from,
        DateTime to
    )
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(predicate)
            .Where(aw => aw.CreatedAt >= from && aw.CreatedAt <= to)
            .CountAsync();
    }

    public Task<List<UserModel>> GetUsersByWorkIdAsync(Ulid workId)
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(aw => aw.WorkId == workId)
            .Select(aw => aw.Student)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<Dictionary<DateTime, int>> GetByDateRangeAsync(
        Expression<Func<AssignedWorkModel, bool>> predicate,
        DateTime from,
        DateTime to
    )
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(predicate)
            .Where(aw => aw.CreatedAt >= from && aw.CreatedAt <= to)
            .GroupBy(aw => aw.CreatedAt.Date)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public Task<Dictionary<DateTime, double?>> GetMonthAverageScoresAsync(
        Ulid studentId,
        WorkType? workType
    )
    {
        return Context
            .Set<AssignedWorkModel>()
            .Where(aw => aw.StudentId == studentId && (workType == null || aw.Type == workType))
            .GroupBy(aw => new { aw.CreatedAt.Year, aw.CreatedAt.Month })
            .ToDictionaryAsync(
                g => new DateTime(g.Key.Year, g.Key.Month, 1),
                g => g.Average(aw => aw.Score)
            );
    }

    public async Task<int> GetCurrentAttemptAsync(Ulid workAssignmentId, Ulid userId)
    {
        return await Context
                .Set<AssignedWorkModel>()
                .Where(aw =>
                    aw.CourseWorkAssignmentId == workAssignmentId && aw.StudentId == userId
                )
                .MaxAsync(aw => (int?)aw.Attempt)
            ?? 0;
    }

    public async Task<AssignedWorksCounts> GetCountsForUserAsync(Ulid userId)
    {
        // One COUNT per tab, each built from the very predicate the list query uses for
        // that tab, so a counter cannot drift from the rows behind it. The counts are
        // cached by the service, and every count runs over the user's rows only (covered
        // by IX_assigned_work_student_id / _main_mentor_id / _helper_mentor_id and the
        // status indexes).
        var userWorks = Context
            .Set<AssignedWorkModel>()
            .Where(AssignedWorkCriteria.ParticipatedBy(userId));

        return new AssignedWorksCounts
        {
            Total = await userWorks.CountAsync(
                AssignedWorkTabCriteria.For(AssignedWorkListTab.All)
            ),
            NotSolved = await userWorks.CountAsync(
                AssignedWorkTabCriteria.For(AssignedWorkListTab.NotSolved)
            ),
            NotChecked = await userWorks.CountAsync(
                AssignedWorkTabCriteria.For(AssignedWorkListTab.NotChecked)
            ),
            Checked = await userWorks.CountAsync(
                AssignedWorkTabCriteria.For(AssignedWorkListTab.Checked)
            ),
        };
    }
}
