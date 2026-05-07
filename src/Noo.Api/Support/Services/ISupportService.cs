using Noo.Api.Support.DTO;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

public interface ISupportService
{
    public Ulid CreateCategory(CreateSupportCategoryDTO dto);
    public Task UpdateCategoryAsync(
        Ulid categoryId,
        JsonPatchDocument<UpdateSupportCategoryDTO> dto
    );
    public void DeleteCategory(Ulid categoryId);
    public Ulid CreateArticle(CreateSupportArticleDTO dto);
    public Task UpdateArticleAsync(Ulid articleId, JsonPatchDocument<UpdateSupportArticleDTO> dto);
    public void DeleteArticle(Ulid articleId);
    public Task<IEnumerable<SupportCategoryModel>> GetCategoryTreeAsync();
    public Task<SupportArticleModel?> GetArticleAsync(Ulid articleId);
}
