using AutoMapper;
using SystemTextJsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.DataAbstraction.Criteria;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Core.Utils.Json;
using Noo.Api.Works.DTO;
using Noo.Api.Works.Models;

namespace Noo.Api.Works.Services;

[RegisterScoped(typeof(IWorkService))]
public class WorkService : IWorkService
{
    protected readonly IUnitOfWork UnitOfWork;

    protected readonly ISearchStrategy<WorkModel> SearchStrategy;

    protected readonly IMapper Mapper;

    public WorkService(IUnitOfWork unitOfWork, IMapper mapper, WorkSearchStrategy searchStrategy)
    {
        UnitOfWork = unitOfWork;
        SearchStrategy = searchStrategy;
        Mapper = mapper;
    }

    public async Task<Ulid> CreateWorkAsync(CreateWorkDTO work)
    {
        var model = Mapper.Map<WorkModel>(work);

        UnitOfWork.WorkRepository().Add(model);
        await UnitOfWork.CommitAsync();

        return model.Id;
    }

    public async Task<WorkDTO?> GetWorkAsync(Ulid id)
    {
        var model = await UnitOfWork.WorkRepository().GetWithTasksAsync(id);

        return Mapper.Map<WorkDTO?>(model);
    }

    public async Task<(IEnumerable<WorkDTO>, int)> GetWorksAsync(Criteria<WorkModel> criteria)
    {
        var (items, total) = await UnitOfWork.WorkRepository().SearchAsync<WorkDTO>(criteria, SearchStrategy, Mapper.ConfigurationProvider);

        return (items, total);
    }

    public async Task UpdateWorkAsync(Ulid id, JsonPatchDocument<UpdateWorkDTO> workUpdatePayload, ModelStateDictionary? modelState = null)
    {
        var repository = UnitOfWork.WorkRepository();
        var model = await repository.GetByIdAsync(id) ?? throw new NotFoundException();

        if (model == null)
        {
            throw new NotFoundException();
        }

        var dto = Mapper.Map<UpdateWorkDTO>(model);

        modelState ??= new ModelStateDictionary();

        workUpdatePayload.ApplyToAndValidate(dto, modelState);

        if (!modelState.IsValid)
        {
            throw new BadRequestException();
        }

        Mapper.Map(dto, model);

        repository.Update(model);
        await UnitOfWork.CommitAsync();
    }

    public async Task DeleteWorkAsync(Ulid id)
    {
        UnitOfWork.WorkRepository().DeleteById(id);

        await UnitOfWork.CommitAsync();
    }
}
