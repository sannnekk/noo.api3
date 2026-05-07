using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Exceptions.Http;
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
    private readonly ISnippetRepository _snippetRepository;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMapper _mapper;

    public SnippetService(
        ISnippetRepository snippetRepository,
        IJsonPatchUpdateService jsonPatchUpdateService,
        IMapper mapper
    )
    {
        _snippetRepository = snippetRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mapper = mapper;
    }

    public void CreateSnippet(Ulid userId, CreateSnippetDTO createSnippetDto)
    {
        var model = _mapper.Map<SnippetModel>(createSnippetDto);
        model.UserId = userId;

        _snippetRepository.Add(model);
    }

    public async Task DeleteSnippetAsync(Ulid userId, Ulid snippetId)
    {
        var snippet = await _snippetRepository.GetByIdAsync(snippetId);
        if (snippet == null)
        {
            return;
        }

        if (snippet.UserId != userId)
        {
            throw new NotFoundException();
        }

        _snippetRepository.Delete(snippet);
    }

    public Task<SearchResult<SnippetModel>> GetSnippetsAsync(Ulid userId)
    {
        var filter = new SnippetFilter
        {
            UserId = userId,
            Page = 1,
            PerPage = SnippetConfig.MaxSnippetsPerUser,
        };

        return _snippetRepository.GetManyAsync(filter);
    }

    public async Task UpdateSnippetAsync(
        Ulid userId,
        Ulid snippetId,
        JsonPatchDocument<UpdateSnippetDTO> updateSnippetDto
    )
    {
        var model = await _snippetRepository.GetAsync(snippetId, userId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, updateSnippetDto);
    }
}
