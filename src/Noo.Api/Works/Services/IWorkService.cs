using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Works.Services;

public interface IWorkService
{
    public Task<WorkModel?> GetWorkAsync(Ulid id);

    public Task<SearchResult<WorkModel>> GetWorksAsync(WorkFilter filter);

    public Ulid CreateWork(CreateWorkDTO work);

    public Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> workUpdatePayload);

    public void DeleteWork(Ulid id);
}
