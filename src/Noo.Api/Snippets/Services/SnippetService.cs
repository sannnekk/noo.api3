using AutoMapper;
using Noo.Api.Core.DataAbstraction.Criteria;
using Noo.Api.Core.DataAbstraction.Criteria.Filters;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions.Http;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Snippets.DTO;
using Noo.Api.Snippets.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Snippets.Services;

[RegisterScoped(typeof(ISnippetService))]
public class SnippetService : ISnippetService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IMapper _mapper;

    public SnippetService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task CreateSnippetAsync(Ulid userId, CreateSnippetDTO createSnippetDto)
    {
        var model = _mapper.Map<SnippetModel>(createSnippetDto);
        model.UserId = userId;

        _unitOfWork.SnippetRepository().Add(model);
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteSnippetAsync(Ulid userId, Ulid snippetId)
    {
        var snippet = await _unitOfWork.SnippetRepository()
            .GetAsync(snippetId, userId);

        if (snippet == null)
        {
            throw new NotFoundException();
        }

        _unitOfWork.SnippetRepository().Delete(snippet);
        await _unitOfWork.CommitAsync();
    }

    public async Task<(IEnumerable<SnippetDTO>, int)> GetSnippetsAsync(Ulid userId)
    {
        var criteria = new Criteria<SnippetModel>();

        criteria.AddFilter(nameof(SnippetModel.UserId), FilterType.Equals, userId);
        criteria.Page = 1;
        criteria.Limit = SnippetConfig.MaxSnippetsPerUser;

        var result = await _unitOfWork.SnippetRepository()
            .GetManyAsync<SnippetDTO>(criteria, _mapper.ConfigurationProvider);

        return (result.Items, result.Total);
    }

    public Task UpdateSnippetAsync(Ulid userId, Ulid snippetId, JsonPatchDocument<UpdateSnippetDTO> updateSnippetDto)
    {
        throw new NotImplementedException();
    }
}
