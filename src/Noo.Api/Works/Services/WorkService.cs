using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Filters;
using Noo.Api.Works.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkService))]
public class WorkService : IWorkService
{
    private readonly IWorkRepository _workRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    public WorkService(
        IWorkRepository workRepository,
        IMapper mapper,
        IJsonPatchUpdateService patchUpdateService
    )
    {
        _workRepository = workRepository;
        _mapper = mapper;
        _patchUpdateService = patchUpdateService;
    }

    public Ulid CreateWork(CreateWorkDTO work)
    {
        var model = _mapper.Map<WorkModel>(work);

        _workRepository.Add(model);

        return model.Id;
    }

    public Task<WorkModel?> GetWorkAsync(Ulid id)
    {
        return _workRepository.GetWithTasksAsync(id);
    }

    public Task<SearchResult<WorkModel>> GetWorksAsync(WorkFilter filter)
    {
        return _workRepository.SearchAsync(filter);
    }

    public async Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> updateWorkDto)
    {
        var workModel = await _workRepository.GetWithTasksAsync(id);

        workModel.ThrowNotFoundIfNull();

        _patchUpdateService.ApplyPatch(workModel, updateWorkDto);
    }

    public void DeleteWork(Ulid id)
    {
        _workRepository.DeleteById(id);
    }
}
