using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using Noo.Api.Works.Types;
using SystemTextJsonPatch;

namespace Noo.Api.Works.Services;

public interface IWorkService
{
    public Task<WorkModel?> GetWorkAsync(Ulid id);

    public Task<SearchResult<WorkModel>> GetWorksAsync(WorkFilter filter);

    public Ulid CreateWork(CreateWorkDTO work);

    public Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> workUpdatePayload);

    public void DeleteWork(Ulid id);

    public Task<WorkStatistics?> GetWorkStatisticsAsync(Ulid id);

    public Task<IEnumerable<WorkRelation>> GetWorkRelationsAsync(Ulid workId);
}
