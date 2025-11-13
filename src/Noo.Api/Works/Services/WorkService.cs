using AutoMapper;
using SystemTextJsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Models;
using Noo.Api.Works.Filters;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.Json;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkService))]
public class WorkService : IWorkService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IWorkRepository _workRepository;

    private readonly IMapper _mapper;

    public WorkService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _workRepository = unitOfWork.WorkRepository();
        _mapper = mapper;
    }

    public async Task<Ulid> CreateWorkAsync(CreateWorkDTO work)
    {
        var model = _mapper.Map<WorkModel>(work);

        _unitOfWork.WorkRepository().Add(model);
        await _unitOfWork.CommitAsync();

        return model.Id;
    }

    public Task<WorkModel?> GetWorkAsync(Ulid id)
    {
        return _unitOfWork.WorkRepository().GetWithTasksAsync(id);
    }

    public Task<SearchResult<WorkModel>> GetWorksAsync(WorkFilter filter)
    {
        return _unitOfWork.WorkRepository().SearchAsync(filter);
    }

    public async Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> updateWorkDto, ModelStateDictionary? modelState = null)
    {
        modelState ??= new ModelStateDictionary();

        var workModel = await _workRepository.GetWithTasksAsync(id);
        if (workModel == null)
        {
            throw new NotFoundException();
        }

        var workDto = _mapper.Map<UpdateWorkDTO>(workModel);
        updateWorkDto.ApplyToAndValidate(workDto, modelState);

        if (!modelState.IsValid)
        {
            throw new BadRequestException("Invalid model state");
        }

        _mapper.Map(workDto, workModel);
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteWorkAsync(Ulid id)
    {
        try
        {
            _unitOfWork.WorkRepository().DeleteById(id);
            await _unitOfWork.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Ignore if the entity was already deleted
        }
    }
}
