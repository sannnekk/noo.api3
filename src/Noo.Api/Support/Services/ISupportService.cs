using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Filters;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

public interface ISupportService
{
    public Ulid CreateArticle(CreateSupportArticleDTO dto);
    public Task UpdateArticleAsync(Ulid articleId, JsonPatchDocument<UpdateSupportArticleDTO> dto);
    public void DeleteArticle(Ulid articleId);
    public Task<SupportArticleModel?> GetArticleAsync(string articleSlug);
    public Task<SearchResult<SupportArticleModel>> GetArticlesAsync(SupportArticleFilter filter);
}
