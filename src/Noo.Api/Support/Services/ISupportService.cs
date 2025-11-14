using Noo.Api.Support.DTO;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

public interface ISupportService
{
    public Task<Ulid> CreateCategoryAsync(CreateSupportCategoryDTO dto);
    public Task UpdateCategoryAsync(Ulid categoryId, JsonPatchDocument<UpdateSupportCategoryDTO> dto);
    public Task DeleteCategoryAsync(Ulid categoryId);
    public Task<Ulid> CreateArticleAsync(CreateSupportArticleDTO dto);
    public Task UpdateArticleAsync(Ulid articleId, JsonPatchDocument<UpdateSupportArticleDTO> dto);
    public Task DeleteArticleAsync(Ulid articleId);
    public Task<IEnumerable<SupportCategoryModel>> GetCategoryTreeAsync();
    public Task<SupportArticleModel?> GetArticleAsync(Ulid articleId);
}
