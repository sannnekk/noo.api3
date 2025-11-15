using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.DTO;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Users.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.AssignedWorks.Services;

[RegisterScoped(typeof(IAssignedWorkRepository))]
public class AssignedWorkRepository : Repository<AssignedWorkModel>, IAssignedWorkRepository
{
    public AssignedWorkRepository(NooDbContext dbContext) : base(dbContext)
    {
    }

    public Task<AssignedWorkProgressDTO?> GetProgressAsync(Ulid assignedWorkId, Ulid? userId)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId && aw.StudentId == userId)
            .Select(aw => new AssignedWorkProgressDTO
            {
                AssignedWorkId = aw.Id,
                SolveStatus = aw.SolveStatus,
                SolvedAt = aw.SolvedAt,
                CheckStatus = aw.CheckStatus,
                CheckedAt = aw.CheckedAt,
                Score = aw.Score,
                MaxScore = aw.MaxScore,
                Attempt = aw.Attempt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId)
    {
        var assignedWork = await Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.Answers)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (assignedWork?.Answers != null)
        {
            foreach (var answer in assignedWork.Answers.Where(a => a.Status == AssignedWorkAnswerStatus.NotSubmitted))
            {
                // Hide mentor-only fields for not submitted answers
                answer.MentorComment = null;
                answer.Score = null;
                answer.DetailedScore = null;
            }
        }

        return assignedWork;
    }

    public Task<AssignedWorkModel?> GetAsync(Ulid assignedWorkId, Ulid? userId)
    {
        if (userId == null)
        {
            return Task.FromResult<AssignedWorkModel?>(null);
        }

        return Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId && (
                aw.StudentId == userId ||
                aw.MainMentorId == userId ||
                aw.HelperMentorId == userId))
            .FirstOrDefaultAsync();
    }

    public Task<AssignedWorkModel?> GetWholeAsync(Ulid assignedWorkId)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.Answers)
            .Include(aw => aw.Student)
            .Include(aw => aw.MainMentor)
            .Include(aw => aw.HelperMentor)
            .Include(aw => aw.Work)
            .ThenInclude(w => w!.Tasks)
            .AsSplitQuery() //! To avoid Cartesian product issues, DO NOT REMOVE
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsMentorOwnWorkAsync(Ulid assignedWorkId, Ulid userId)
    {
        var assignedWork = await Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .FirstOrDefaultAsync();

        return assignedWork != null && (assignedWork.MainMentorId == userId || assignedWork.HelperMentorId == userId);
    }

    public async Task<bool> IsStudentOwnWorkAsync(Ulid assignedWorkId, Ulid userId)
    {
        var assignedWork = await Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .FirstOrDefaultAsync();

        return assignedWork != null && assignedWork.StudentId == userId;
    }

    public async Task<bool> IsWorkCheckStatusAsync(Ulid assignedWorkId, params AssignedWorkCheckStatus[] statuses)
    {
        var assignedWork = await Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .FirstOrDefaultAsync();

        return assignedWork != null && statuses.Contains(assignedWork.CheckStatus);
    }

    public async Task<bool> IsWorkSolveStatusAsync(Ulid assignedWorkId, params AssignedWorkSolveStatus[] statuses)
    {
        var assignedWork = await Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .FirstOrDefaultAsync();

        return assignedWork != null && statuses.Contains(assignedWork.SolveStatus);
    }

    public Task<AssignedWorkModel?> GetWithStudentAsync(Ulid assignedWorkId)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(aw => aw.Id == assignedWorkId)
            .Include(aw => aw.Student)
            .FirstOrDefaultAsync();
    }

    public Task<int> GetCountAsync(Expression<Func<AssignedWorkModel, bool>> predicate, DateTime from, DateTime to)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(predicate)
            .Where(aw => aw.CreatedAt >= from && aw.CreatedAt <= to)
            .CountAsync();
    }

    public Task<List<UserModel>> GetUsersByWorkIdAsync(Ulid workId)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(aw => aw.WorkId == workId)
            .Select(aw => aw.Student)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<Dictionary<DateTime, int>> GetByDateRangeAsync(Expression<Func<AssignedWorkModel, bool>> predicate, DateTime from, DateTime to)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(predicate)
            .Where(aw => aw.CheckDeadlineAt >= aw.CheckedAt && aw.CreatedAt >= from && aw.CreatedAt <= to)
            .GroupBy(aw => aw.CreatedAt.Date)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public Task<Dictionary<DateTime, double?>> GetMonthAverageScoresAsync(Ulid studentId, WorkType? workType)
    {
        return Context.Set<AssignedWorkModel>()
            .Where(aw => aw.StudentId == studentId && (workType == null || aw.Type == workType))
            .GroupBy(aw => new { aw.CreatedAt.Year, aw.CreatedAt.Month })
            .ToDictionaryAsync(g => new DateTime(g.Key.Year, g.Key.Month, 1), g => g.Average(aw => aw.Score));
    }
}
