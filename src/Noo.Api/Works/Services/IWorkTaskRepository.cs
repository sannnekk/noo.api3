using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Works.Models;

namespace Noo.Api.Works.Services;

public interface IWorkTaskRepository : IRepository<WorkTaskModel>
{
    public Task<int> GetWorkMaxScoreAsync(Ulid workId);
}
