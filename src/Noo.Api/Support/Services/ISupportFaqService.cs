using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Support.DTO;
using Noo.Api.Support.Filters;
using Noo.Api.Support.Models;
using SystemTextJsonPatch;

namespace Noo.Api.Support.Services;

public interface ISupportFaqService
{
    public Ulid CreateItem(CreateSupportFaqItemDTO dto);
    public Task UpdateItemAsync(Ulid itemId, JsonPatchDocument<UpdateSupportFaqItemDTO> dto);
    public void DeleteItem(Ulid itemId);
    public Task<SearchResult<SupportFaqItemModel>> GetItemsAsync(SupportFaqItemFilter filter);
}
