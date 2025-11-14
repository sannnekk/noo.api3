using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Snippets.DTO;
using Noo.Api.Snippets.Filters;
using Noo.Api.Snippets.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Snippets.Services;

[RegisterScoped(typeof(ISnippetService))]
public class SnippetService : ISnippetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISnippetRepository _snippetRepository;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMapper _mapper;

    public SnippetService(IUnitOfWork unitOfWork, ISnippetRepository snippetRepository, IJsonPatchUpdateService jsonPatchUpdateService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _snippetRepository = snippetRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mapper = mapper;
    }

    public async Task CreateSnippetAsync(Ulid userId, CreateSnippetDTO createSnippetDto)
    {
        var model = _mapper.Map<SnippetModel>(createSnippetDto);
        model.UserId = userId;

        _snippetRepository.Add(model);
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteSnippetAsync(Ulid userId, Ulid snippetId)
    {
        var snippet = await _snippetRepository.GetAsync(snippetId, userId);

        snippet.ThrowNotFoundIfNull();

        _snippetRepository.Delete(snippet);
        await _unitOfWork.CommitAsync();
    }

    public Task<SearchResult<SnippetModel>> GetSnippetsAsync(Ulid userId)
    {
        var filter = new SnippetFilter
        {
            UserId = userId,
            Page = 1,
            PerPage = SnippetConfig.MaxSnippetsPerUser
        };

        return _snippetRepository.GetManyAsync(filter);
    }

    public async Task UpdateSnippetAsync(Ulid userId, Ulid snippetId, JsonPatchDocument<UpdateSnippetDTO> updateSnippetDto)
    {
        var model = await _snippetRepository.GetAsync(snippetId, userId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updateSnippetDto);

        _snippetRepository.Update(model);
        await _unitOfWork.CommitAsync();
    }
}
