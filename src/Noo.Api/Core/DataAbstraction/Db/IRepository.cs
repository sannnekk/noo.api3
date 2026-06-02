using Ardalis.Specification;
using AutoFilterer.Abstractions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Noo.Api.Core.DataAbstraction.Model;
using SystemTextJsonPatch;

namespace Noo.Api.Core.DataAbstraction.Db;

public interface IRepository<T>
    where T : BaseModel
{
    public NooDbContext Context { get; init; }

    public Task<T?> GetByIdAsync(Ulid id);

    public Task<SearchResult<T>> SearchAsync(
        IPaginationFilter filter,
        IEnumerable<ISpecification<T>>? specifications = default
    );

    public Task<SearchResult<T>> GetManyAsync(IPaginationFilter filter);

    public Task<bool> ExistsAsync(Ulid id);

    public void Add(T entity);

    public void Delete(T entity);

    public void DeleteById(Ulid id);

    public Task UpdateWithJsonPatchAsync<TDto>(
        Ulid id,
        JsonPatchDocument<TDto> updateDto,
        IMapper mapper,
        ModelStateDictionary? modelState
    )
        where TDto : class;
}
