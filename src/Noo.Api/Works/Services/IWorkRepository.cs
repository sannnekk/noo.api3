using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;

namespace Noo.Api.Works.Services;

public interface IWorkRepository : IRepository<WorkModel>
{
    public Task<WorkModel?> GetWithTasksAsync(Ulid id);

    public Task<int> CountSolvedAsync(Ulid id);

    public Task<List<int>> GetScoresAsync(Ulid id);

    public Task<IReadOnlyList<TaskSummary>> GetTaskSummariesAsync(Ulid id);

    public Task<IEnumerable<WorkRelation>> GetWorkRelationsAsync(Ulid workId);
}
