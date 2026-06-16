using Microsoft.EntityFrameworkCore;
using Noo.Api.AssignedWorks.Models;
using Noo.Api.AssignedWorks.Types;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkRepository))]
public class WorkRepository : Repository<WorkModel>, IWorkRepository
{
    public WorkRepository(NooDbContext context)
        : base(context) { }

    public Task<WorkModel?> GetWithTasksAsync(Ulid id)
    {
        return Context
            .GetDbSet<WorkModel>()
            .Include(x => x.Tasks!.OrderBy(task => task.Order))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<int> CountSolvedAsync(Ulid id)
    {
        return AssignedWorksOf(id)
            .CountAsync(aw => aw.SolveStatus == AssignedWorkSolveStatus.Solved);
    }

    public Task<List<int>> GetScoresAsync(Ulid id)
    {
        return AssignedWorksOf(id)
            .Where(aw => aw.Score != null)
            .Select(aw => aw.Score!.Value)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TaskSummary>> GetTaskSummariesAsync(Ulid id)
    {
        var maxScores = await Context
            .GetDbSet<WorkTaskModel>()
            .Where(task => task.WorkId == id)
            .Select(task => new { task.Id, task.MaxScore })
            .ToListAsync();

        var averageScores = await GetTaskAverageScoresAsync(id);

        return maxScores
            .Select(task => new TaskSummary
            {
                TaskId = task.Id,
                MaxScore = task.MaxScore,
                AverageScore = averageScores.TryGetValue(task.Id, out var average)
                    ? average
                    : null,
            })
            .OrderBy(summary =>
                summary.AverageScore is double average && summary.MaxScore > 0
                    ? average / summary.MaxScore
                    : double.MaxValue
            )
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Ulid, double>> GetTaskAverageScoresAsync(Ulid id)
    {
        return await Context
            .GetDbSet<AssignedWorkAnswerModel>()
            .Where(answer => answer.AssignedWork.WorkId == id && answer.Score != null)
            .GroupBy(answer => answer.TaskId)
            .Select(group => new
            {
                TaskId = group.Key,
                Average = group.Average(answer => answer.Score!.Value),
            })
            .ToDictionaryAsync(entry => entry.TaskId, entry => entry.Average);
    }

    private IQueryable<AssignedWorkModel> AssignedWorksOf(Ulid workId)
    {
        return Context.GetDbSet<AssignedWorkModel>().Where(aw => aw.WorkId == workId);
    }
}
