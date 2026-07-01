using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using AutoFilterer.Abstractions;
using AutoFilterer.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Noo.Api.Core.DataAbstraction.Model;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.Json;
using SystemTextJsonPatch;

namespace Noo.Api.Core.DataAbstraction.Db;

public class Repository<T> : IRepository<T>
    where T : BaseModel, new()
{
    public NooDbContext Context { get; init; }

    public Repository(NooDbContext context)
    {
        Context = context;
    }

    public Task<T?> GetByIdAsync(Ulid id)
    {
        return Context.GetDbSet<T>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public void Add(T entity)
    {
        Context.GetDbSet<T>().Add(entity);
    }

    public Task<bool> ExistsAsync(Ulid id)
    {
        return Context.GetDbSet<T>().AnyAsync(e => e.Id == id);
    }

    public void Delete(T entity)
    {
        Context.GetDbSet<T>().Remove(entity);
    }

    public void DeleteById(Ulid id)
    {
        DeleteEntity(new T { Id = id });
    }

    protected void DeleteEntity(T entity)
    {
        var set = Context.GetDbSet<T>();

        // If an entity with the same key is already tracked, remove that instance to avoid tracking conflicts
        var trackedEntity = set.Local.FirstOrDefault(e => e.Id == entity.Id);
        if (trackedEntity != null)
        {
            set.Remove(trackedEntity);
            return;
        }

        // Otherwise, attach the entity and remove it
        set.Attach(entity);
        set.Remove(entity);
    }

    public async Task<SearchResult<T>> SearchAsync(
        IPaginationFilter filter,
        IEnumerable<ISpecification<T>>? specifications = default
    )
    {
        var query = Context.GetDbSet<T>().AsQueryable();

        if (specifications != null)
        {
            foreach (var spec in specifications)
            {
                query = query.WithSpecification(spec);
            }
        }

        var total = await query.ApplyFilterWithoutPagination(filter).CountAsync();

        var results = await query.ApplyDefaultOrdering(filter).ApplyFilter(filter).ToListAsync();

        return new SearchResult<T>(results, total);
    }

    public async Task<SearchResult<T>> GetManyAsync(IPaginationFilter filter)
    {
        var query = Context.GetDbSet<T>().AsQueryable();

        var total = await query.ApplyFilterWithoutPagination(filter).CountAsync();

        var results = await query.ApplyDefaultOrdering(filter).ApplyFilter(filter).ToListAsync();

        return new SearchResult<T>(results, total);
    }

    public async Task UpdateWithJsonPatchAsync<TDto>(
        Ulid id,
        JsonPatchDocument<TDto> updateDto,
        IMapper mapper,
        ModelStateDictionary? modelState = null
    )
        where TDto : class
    {
        var model = await GetByIdAsync(id) ?? throw new NotFoundException();

        if (model == null)
        {
            throw new NotFoundException();
        }

        var dto = mapper.Map<TDto>(model);

        modelState ??= new ModelStateDictionary();

        updateDto.ApplyToAndValidate(dto, modelState);

        if (!modelState.IsValid)
        {
            throw new BadRequestException();
        }

        mapper.Map(dto, model);
    }
}
