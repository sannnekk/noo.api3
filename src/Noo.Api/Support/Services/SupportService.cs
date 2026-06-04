using AutoMapper;
using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Exceptions;
using Noo.Api.Core.Request.Patching;
using Noo.Api.Core.Utils.DI;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Filters;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

[RegisterScoped(typeof(ISupportService))]
public class SupportService : ISupportService
{
    private readonly ISupportArticleRepository _articleRepository;
    private readonly IJsonPatchUpdateService _jsonPatchUpdateService;
    private readonly IMapper _mapper;

    public SupportService(
        ISupportArticleRepository articleRepository,
        IJsonPatchUpdateService jsonPatchUpdateService,
        IMapper mapper
    )
    {
        _articleRepository = articleRepository;
        _jsonPatchUpdateService = jsonPatchUpdateService;
        _mapper = mapper;
    }

    public Ulid CreateArticle(CreateSupportArticleDTO dto)
    {
        var model = _mapper.Map<SupportArticleModel>(dto);

        _articleRepository.Add(model);

        return model.Id;
    }

    public void DeleteArticle(Ulid articleId)
    {
        _articleRepository.DeleteById(articleId);
    }

    public Task<SearchResult<SupportArticleModel>> GetArticlesAsync(SupportArticleFilter filter)
    {
        return _articleRepository.SearchAsync(filter);
    }

    public Task<SupportArticleModel?> GetArticleAsync(string articleSlug)
    {
        return _articleRepository.GetBySlugAsync(articleSlug);
    }

    public async Task UpdateArticleAsync(
        Ulid articleId,
        JsonPatchDocument<UpdateSupportArticleDTO> dto
    )
    {
        var model = await _articleRepository.GetByIdAsync(articleId);

        model.ThrowNotFoundIfNull();

        _jsonPatchUpdateService.ApplyPatch(model, dto);
    }
}
