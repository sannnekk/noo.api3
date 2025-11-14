using AutoMapper;
using SystemTextJsonPatch;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Models;
using Noo.Api.Works.Filters;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Exceptions;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkService))]
public class WorkService : IWorkService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IWorkRepository _workRepository;

    private readonly IMapper _mapper;

    private readonly IJsonPatchUpdateService _patchUpdateService;

    public WorkService(IUnitOfWork unitOfWork, IWorkRepository workRepository, IMapper mapper, IJsonPatchUpdateService patchUpdateService)
    {
        _unitOfWork = unitOfWork;
        _workRepository = workRepository;
        _mapper = mapper;
        _patchUpdateService = patchUpdateService;
    }

    public async Task<Ulid> CreateWorkAsync(CreateWorkDTO work)
    {
        var model = _mapper.Map<WorkModel>(work);

        _workRepository.Add(model);
        await _unitOfWork.CommitAsync();

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
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteWorkAsync(Ulid id)
    {
        try
        {
            _workRepository.DeleteById(id);
            await _unitOfWork.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Ignore if the entity was already deleted
        }
    }
}
